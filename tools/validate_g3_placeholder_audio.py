#!/usr/bin/env python3
"""Independently validate the G3 local-synth placeholder audio package."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import sys
import wave
from dataclasses import dataclass
from pathlib import Path

import numpy as np


SAMPLE_RATE = 48_000
BIT_DEPTH = 24
GENERATOR_VERSION = "g3-local-synth-v0.1"
MASTER_SEED = 0x5350495245434845
SAMPLE_PEAK_CEILING_DBFS = -3.0
EXPECTED_RUNTIME_CLIPS = 67
MAX_SAMPLE_PEAK_DBFS = -2.99
MIN_RMS_DBFS = -30.0
MAX_RMS_DBFS = -16.0
MAX_DC_OFFSET = 0.0001
MAX_SFX_LEAD_SECONDS = 0.010
MAX_LOOP_SEAM_DELTA = 0.00001
MAX_LOOP_SLOPE_DELTA = 0.01
MAX_VARIANT_RMS_SPREAD_DB = 0.5
SOURCE_DESCRIPTION = (
    "Deterministic procedural synthesis; oscillators, filtered noise, "
    "and mathematical envelopes only; no external samples."
)


@dataclass(frozen=True)
class ExpectedFile:
    cue_id: str
    variant: int
    bus: str
    channels: int
    sample_count: int
    loop_start_sample: int | None = None
    loop_end_sample: int | None = None
    mode: str | None = None
    bpm: int | None = None

    @property
    def duration_seconds(self) -> float:
        return self.sample_count / SAMPLE_RATE


# This matrix is deliberately duplicated rather than imported from the
# generator. Drift between generation and validation must fail.
BGM_CONTRACT: dict[str, tuple[int, str, int]] = {
    "bgm_main_menu": (72, "D Dorian", 5_120_000),
    "bgm_run_shop": (90, "G Dorian", 6_144_000),
    "bgm_battle_normal": (120, "D Aeolian", 4_608_000),
}

SFX_CONTRACT: dict[str, tuple[str, int, float, int]] = {
    "ui_click": ("UI", 3, 0.10, 1),
    "ui_confirm": ("UI", 2, 0.24, 1),
    "ui_cancel": ("UI", 2, 0.20, 1),
    "ui_error": ("UI", 2, 0.32, 1),
    "shop_refresh": ("Shop", 3, 0.48, 2),
    "shop_buy": ("Shop", 3, 0.35, 1),
    "shop_sell": ("Shop", 3, 0.38, 1),
    "shop_play": ("Shop", 3, 0.44, 1),
    "shop_spell": ("Shop", 3, 0.65, 1),
    "shop_triple": ("Shop", 1, 1.15, 1),
    "shop_discover_open": ("Shop", 1, 0.80, 2),
    "shop_discover_pick": ("Shop", 2, 0.38, 1),
    "shop_upgrade": ("Shop", 1, 1.10, 1),
    "battle_attack_light": ("Battle", 4, 0.28, 1),
    "battle_hit": ("Battle", 4, 0.30, 1),
    "battle_shield_gain": ("Battle", 3, 0.50, 1),
    "battle_shield_break": ("Battle", 3, 0.55, 1),
    "battle_stat_up": ("Battle", 3, 0.42, 1),
    "battle_death": ("Battle", 4, 0.90, 1),
    "battle_token_death": ("Battle", 3, 0.35, 1),
    "battle_summon": ("Battle", 4, 0.55, 1),
    "battle_victory": ("Battle", 1, 1.35, 1),
    "battle_defeat": ("Battle", 1, 1.35, 1),
    "run_node_select": ("Run", 3, 0.34, 1),
    "run_reward": ("Run", 2, 0.85, 1),
}


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def expected_paths() -> dict[str, ExpectedFile]:
    result: dict[str, ExpectedFile] = {}
    for cue_id, (bpm, mode, sample_count) in BGM_CONTRACT.items():
        relative = (
            Path("Music")
            / "Placeholder"
            / f"placeholder_{cue_id}_v01.wav"
        )
        result[relative.as_posix()] = ExpectedFile(
            cue_id=cue_id,
            variant=1,
            bus="Music",
            channels=2,
            sample_count=sample_count,
            loop_start_sample=0,
            loop_end_sample=sample_count,
            mode=mode,
            bpm=bpm,
        )
    for cue_id, (
        domain,
        variants,
        duration,
        channels,
    ) in SFX_CONTRACT.items():
        sample_count = round(duration * SAMPLE_RATE)
        for variant in range(1, variants + 1):
            relative = (
                Path("SFX")
                / domain
                / "Placeholder"
                / f"placeholder_sfx_{cue_id}_{variant:02d}.wav"
            )
            result[relative.as_posix()] = ExpectedFile(
                cue_id=cue_id,
                variant=variant,
                bus="UI" if domain == "UI" else "SFX",
                channels=channels,
                sample_count=sample_count,
            )
    return result


def decode_pcm24(raw: bytes, channels: int) -> np.ndarray:
    octets = np.frombuffer(raw, dtype=np.uint8).reshape(-1, 3)
    values = (
        octets[:, 0].astype(np.int32)
        | octets[:, 1].astype(np.int32) << 8
        | octets[:, 2].astype(np.int32) << 16
    )
    values = (values ^ 0x800000) - 0x800000
    return values.reshape(-1, channels).astype(np.float64) / 8_388_608.0


def inspect_wave(path: Path) -> dict[str, float | int]:
    peak = 0.0
    square_sum = 0.0
    scalar_sum = 0.0
    scalar_count = 0
    first_frame: np.ndarray | None = None
    second_frame: np.ndarray | None = None
    penultimate_frame: np.ndarray | None = None
    last_frame: np.ndarray | None = None
    first_active_frame: int | None = None
    processed_frames = 0
    with wave.open(str(path), "rb") as source:
        channels = source.getnchannels()
        sample_width = source.getsampwidth()
        sample_rate = source.getframerate()
        frame_count = source.getnframes()
        while True:
            raw = source.readframes(65_536)
            if not raw:
                break
            samples = decode_pcm24(raw, channels)
            if first_frame is None:
                first_frame = samples[0].copy()
                if samples.shape[0] > 1:
                    second_frame = samples[1].copy()
            if first_active_frame is None:
                active = np.flatnonzero(
                    np.max(np.abs(samples), axis=1) > 0.0001
                )
                if active.size:
                    first_active_frame = (
                        processed_frames + int(active[0])
                    )
            if samples.shape[0] > 1:
                penultimate_frame = samples[-2].copy()
            elif last_frame is not None:
                penultimate_frame = last_frame.copy()
            last_frame = samples[-1].copy()
            peak = max(peak, float(np.max(np.abs(samples))))
            square_sum += float(np.sum(np.square(samples)))
            scalar_sum += float(np.sum(samples))
            scalar_count += samples.size
            processed_frames += samples.shape[0]

    rms = math.sqrt(square_sum / max(1, scalar_count))
    seam = (
        float(np.max(np.abs(first_frame - last_frame)))
        if first_frame is not None and last_frame is not None
        else 0.0
    )
    slope_delta = (
        float(
            np.max(
                np.abs(
                    (second_frame - first_frame)
                    - (last_frame - penultimate_frame)
                )
            )
        )
        if (
            first_frame is not None
            and second_frame is not None
            and penultimate_frame is not None
            and last_frame is not None
        )
        else 0.0
    )
    return {
        "channels": channels,
        "sampleWidthBytes": sample_width,
        "sampleRate": sample_rate,
        "sampleCount": frame_count,
        "samplePeakDbfs": 20.0 * math.log10(max(peak, 1e-12)),
        "rmsDbfs": 20.0 * math.log10(max(rms, 1e-12)),
        "dcOffset": scalar_sum / max(1, scalar_count),
        "firstActiveSeconds": (
            first_active_frame / SAMPLE_RATE
            if first_active_frame is not None
            else math.inf
        ),
        "seamDelta": seam,
        "slopeDelta": slope_delta,
    }


def close_enough(
    actual: object,
    expected: float,
    tolerance: float = 0.000001,
) -> bool:
    try:
        return abs(float(actual) - expected) <= tolerance
    except (TypeError, ValueError):
        return False


def validate_manifest_entry(
    relative: str,
    entry: dict[str, object],
    expected: ExpectedFile,
    errors: list[str],
) -> None:
    exact_fields = {
        "cueId": expected.cue_id,
        "variant": expected.variant,
        "bus": expected.bus,
        "assetStatus": "Placeholder",
        "channels": expected.channels,
        "sampleCount": expected.sample_count,
        "loopStartSample": expected.loop_start_sample,
        "loopEndSample": expected.loop_end_sample,
    }
    for field, expected_value in exact_fields.items():
        if entry.get(field) != expected_value:
            errors.append(
                f"{relative}: manifest {field} must be "
                f"{expected_value!r}, found {entry.get(field)!r}."
            )
    if not close_enough(
        entry.get("durationSeconds"),
        expected.duration_seconds,
    ):
        errors.append(f"{relative}: manifest durationSeconds is incorrect.")
    if expected.mode is not None:
        if entry.get("mode") != expected.mode:
            errors.append(f"{relative}: manifest mode is incorrect.")
        if entry.get("bpm") != expected.bpm:
            errors.append(f"{relative}: manifest bpm is incorrect.")
    elif "mode" in entry or "bpm" in entry:
        errors.append(f"{relative}: SFX entry must not define mode or bpm.")


def validate(output_root: Path) -> list[str]:
    errors: list[str] = []
    manifest_path = (
        output_root
        / "Placeholder"
        / "placeholder_audio_manifest.json"
    )
    if not manifest_path.is_file():
        return [f"Missing manifest: {manifest_path}"]

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    top_level_contract = {
        "schemaVersion": 1,
        "generatorVersion": GENERATOR_VERSION,
        "masterSeed": f"0x{MASTER_SEED:016X}",
        "generatedAssetStatus": "Local Synth Placeholder",
        "productionReady": False,
        "source": SOURCE_DESCRIPTION,
        "sampleRate": SAMPLE_RATE,
        "bitDepth": BIT_DEPTH,
        "samplePeakCeilingDbfs": SAMPLE_PEAK_CEILING_DBFS,
        "bgmCueCount": 3,
        "sfxCueCount": 25,
        "sfxClipCount": 64,
        "runtimeClipCount": EXPECTED_RUNTIME_CLIPS,
    }
    for field, expected_value in top_level_contract.items():
        if manifest.get(field) != expected_value:
            errors.append(
                f"Manifest {field} must be {expected_value!r}, "
                f"found {manifest.get(field)!r}."
            )

    generation_runtime = manifest.get("generationRuntime")
    if not isinstance(generation_runtime, dict):
        errors.append("Manifest generationRuntime must be an object.")
    else:
        for field in (
            "pythonImplementation",
            "pythonVersion",
            "numpyVersion",
        ):
            if not isinstance(generation_runtime.get(field), str) or not (
                generation_runtime[field].strip()
            ):
                errors.append(
                    f"Manifest generationRuntime.{field} must be a "
                    "non-empty string."
                )

    expected = expected_paths()
    if len(expected) != EXPECTED_RUNTIME_CLIPS:
        errors.append("Validator contract does not contain exactly 67 clips.")
    if len(BGM_CONTRACT) != 3 or len(SFX_CONTRACT) != 25:
        errors.append("Validator cue contract is not exactly 3 + 25 cues.")

    entries = manifest.get("files", [])
    if not isinstance(entries, list):
        return errors + ["Manifest files must be an array."]
    manifest_by_path: dict[str, dict[str, object]] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            errors.append("Manifest files contains a non-object entry.")
            continue
        relative = entry.get("relativePath")
        if not isinstance(relative, str):
            errors.append("Manifest contains an entry without relativePath.")
            continue
        if relative in manifest_by_path:
            errors.append(f"Duplicate manifest path: {relative}")
        manifest_by_path[relative] = entry

    if set(manifest_by_path) != set(expected):
        missing = sorted(set(expected) - set(manifest_by_path))
        unexpected = sorted(set(manifest_by_path) - set(expected))
        if missing:
            errors.append("Manifest missing paths: " + ", ".join(missing))
        if unexpected:
            errors.append(
                "Manifest has unexpected paths: " + ", ".join(unexpected)
            )

    for relative, expected_file in expected.items():
        entry = manifest_by_path.get(relative)
        if entry is not None:
            validate_manifest_entry(
                relative,
                entry,
                expected_file,
                errors,
            )

    disk_paths = {
        path.relative_to(output_root).as_posix()
        for path in output_root.rglob("*")
        if (
            path.is_file()
            and "Placeholder" in path.relative_to(output_root).parts
            and path.suffix.lower() in {".wav", ".ogg", ".mp3"}
        )
    }
    if disk_paths != set(expected):
        missing = sorted(set(expected) - disk_paths)
        unexpected = sorted(disk_paths - set(expected))
        if missing:
            errors.append("Disk missing audio files: " + ", ".join(missing))
        if unexpected:
            errors.append(
                "Unexpected placeholder audio files: "
                + ", ".join(unexpected)
            )

    rms_by_cue: dict[str, list[float]] = {}
    for relative, expected_file in expected.items():
        path = output_root / relative
        if not path.is_file():
            continue
        try:
            metrics = inspect_wave(path)
        except (ValueError, wave.Error) as exception:
            errors.append(f"{relative}: invalid WAV: {exception}")
            continue

        entry = manifest_by_path.get(relative, {})
        if metrics["channels"] != expected_file.channels:
            errors.append(
                f"{relative}: expected {expected_file.channels} channels, "
                f"found {metrics['channels']}."
            )
        if metrics["sampleWidthBytes"] != 3:
            errors.append(f"{relative}: source WAV is not 24-bit PCM.")
        if metrics["sampleRate"] != SAMPLE_RATE:
            errors.append(f"{relative}: source WAV is not 48000 Hz.")
        if metrics["sampleCount"] != expected_file.sample_count:
            errors.append(
                f"{relative}: expected {expected_file.sample_count} frames, "
                f"found {metrics['sampleCount']}."
            )
        if metrics["samplePeakDbfs"] > MAX_SAMPLE_PEAK_DBFS:
            errors.append(
                f"{relative}: peak {metrics['samplePeakDbfs']:.4f} dBFS "
                "exceeds the -3 dBFS placeholder ceiling."
            )
        if not (
            MIN_RMS_DBFS
            <= metrics["rmsDbfs"]
            <= MAX_RMS_DBFS
        ):
            errors.append(
                f"{relative}: RMS {metrics['rmsDbfs']:.4f} dBFS is outside "
                f"{MIN_RMS_DBFS:.1f}..{MAX_RMS_DBFS:.1f} dBFS."
            )
        if abs(float(metrics["dcOffset"])) > MAX_DC_OFFSET:
            errors.append(
                f"{relative}: DC offset {metrics['dcOffset']:.8f} is too large."
            )
        if (
            expected_file.bus != "Music"
            and metrics["firstActiveSeconds"] >= MAX_SFX_LEAD_SECONDS
        ):
            errors.append(
                f"{relative}: leading silence is "
                f"{metrics['firstActiveSeconds']:.6f} seconds."
            )
        if expected_file.bus == "Music":
            if metrics["seamDelta"] > MAX_LOOP_SEAM_DELTA:
                errors.append(
                    f"{relative}: loop seam delta "
                    f"{metrics['seamDelta']:.8f} is too large."
                )
            if metrics["slopeDelta"] > MAX_LOOP_SLOPE_DELTA:
                errors.append(
                    f"{relative}: loop slope delta "
                    f"{metrics['slopeDelta']:.8f} is too large."
                )

        metric_pairs = (
            ("samplePeakDbfs", "samplePeakDbfs", 0.001),
            ("rmsDbfs", "rmsDbfs", 0.001),
            ("seamDelta", "seamDelta", 0.000001),
        )
        for manifest_field, metric_field, tolerance in metric_pairs:
            if not close_enough(
                entry.get(manifest_field),
                float(metrics[metric_field]),
                tolerance,
            ):
                errors.append(
                    f"{relative}: manifest {manifest_field} has drifted."
                )
        if entry.get("sha256") != sha256_file(path):
            errors.append(f"{relative}: SHA-256 mismatch.")

        rms_by_cue.setdefault(
            expected_file.cue_id,
            [],
        ).append(float(metrics["rmsDbfs"]))

    for cue_id, values in rms_by_cue.items():
        if values and max(values) - min(values) > MAX_VARIANT_RMS_SPREAD_DB:
            errors.append(
                f"{cue_id}: variant RMS spread is "
                f"{max(values) - min(values):.4f} dB."
            )

    return errors


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output-root",
        type=Path,
        default=(
            repo_root
            / "sc"
            / "Assets"
            / "Audio"
            / "Presentation"
        ),
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    errors = validate(args.output_root.resolve())
    if errors:
        print("G3 placeholder audio validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print(
        "G3 placeholder audio validation passed independently: "
        "3 BGM + 25 SFX cues / 64 SFX variants = 67 clips; "
        "48 kHz 24-bit; productionReady=false."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
