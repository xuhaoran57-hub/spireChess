#!/usr/bin/env python3
"""Generate deterministic G3 placeholder music and sound effects.

The output is deliberately marked as Local Synth Placeholder. It contains no
external samples and must never satisfy the production audio gate.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import platform
import wave
from dataclasses import dataclass
from pathlib import Path

import numpy as np


SAMPLE_RATE = 48_000
BIT_DEPTH = 24
GENERATOR_VERSION = "g3-local-synth-v0.1"
MASTER_SEED = 0x5350495245434845
PEAK_LIMIT_DB = -3.0


@dataclass(frozen=True)
class SfxSpec:
    domain: str
    variants: int
    duration: float
    stereo: bool = False


@dataclass(frozen=True)
class BgmSpec:
    bpm: int
    bars: int
    mode: str
    target_rms_db: float

    @property
    def samples_per_beat(self) -> int:
        return round(SAMPLE_RATE * 60 / self.bpm)

    @property
    def sample_count(self) -> int:
        return self.samples_per_beat * 4 * self.bars


SFX_SPECS: dict[str, SfxSpec] = {
    "ui_click": SfxSpec("UI", 3, 0.10),
    "ui_confirm": SfxSpec("UI", 2, 0.24),
    "ui_cancel": SfxSpec("UI", 2, 0.20),
    "ui_error": SfxSpec("UI", 2, 0.32),
    "shop_refresh": SfxSpec("Shop", 3, 0.48, True),
    "shop_buy": SfxSpec("Shop", 3, 0.35),
    "shop_sell": SfxSpec("Shop", 3, 0.38),
    "shop_play": SfxSpec("Shop", 3, 0.44),
    "shop_spell": SfxSpec("Shop", 3, 0.65),
    "shop_triple": SfxSpec("Shop", 1, 1.15),
    "shop_discover_open": SfxSpec("Shop", 1, 0.80, True),
    "shop_discover_pick": SfxSpec("Shop", 2, 0.38),
    "shop_upgrade": SfxSpec("Shop", 1, 1.10),
    "battle_attack_light": SfxSpec("Battle", 4, 0.28),
    "battle_hit": SfxSpec("Battle", 4, 0.30),
    "battle_shield_gain": SfxSpec("Battle", 3, 0.50),
    "battle_shield_break": SfxSpec("Battle", 3, 0.55),
    "battle_stat_up": SfxSpec("Battle", 3, 0.42),
    "battle_death": SfxSpec("Battle", 4, 0.90),
    "battle_token_death": SfxSpec("Battle", 3, 0.35),
    "battle_summon": SfxSpec("Battle", 4, 0.55),
    "battle_victory": SfxSpec("Battle", 1, 1.35),
    "battle_defeat": SfxSpec("Battle", 1, 1.35),
    "run_node_select": SfxSpec("Run", 3, 0.34),
    "run_reward": SfxSpec("Run", 2, 0.85),
}

BGM_SPECS: dict[str, BgmSpec] = {
    "bgm_main_menu": BgmSpec(72, 32, "D Dorian", -21.0),
    "bgm_run_shop": BgmSpec(90, 48, "G Dorian", -20.0),
    "bgm_battle_normal": BgmSpec(120, 48, "D Aeolian", -19.5),
}


def stable_seed(name: str, variant: int = 0) -> int:
    payload = (
        f"SpireChess/{GENERATOR_VERSION}/{name}/{variant:02d}"
    ).encode("utf-8")
    digest = hashlib.sha256(payload).digest()
    return int.from_bytes(digest[:8], "little") ^ MASTER_SEED


def midi_frequency(note: float) -> float:
    return 440.0 * 2.0 ** ((note - 69.0) / 12.0)


def oscillator(
    frequency: float,
    duration: float,
    waveform: str = "sine",
    phase: float = 0.0,
    end_frequency: float | None = None,
) -> np.ndarray:
    frame_count = max(1, round(duration * SAMPLE_RATE))
    time = np.arange(frame_count, dtype=np.float64) / SAMPLE_RATE
    if end_frequency is None or abs(end_frequency - frequency) < 1e-9:
        angle = 2.0 * math.pi * frequency * time + phase
    else:
        slope = (end_frequency - frequency) / max(duration, 1e-9)
        angle = (
            2.0
            * math.pi
            * (frequency * time + 0.5 * slope * time * time)
            + phase
        )
    sine = np.sin(angle)
    if waveform == "triangle":
        return (2.0 / math.pi * np.arcsin(sine)).astype(np.float32)
    if waveform == "soft_square":
        return (
            np.tanh(1.6 * sine) / math.tanh(1.6)
        ).astype(np.float32)
    return sine.astype(np.float32)


def shaped_envelope(
    frame_count: int,
    attack_seconds: float,
    release_seconds: float,
    decay_rate: float = 3.0,
) -> np.ndarray:
    if frame_count <= 0:
        return np.zeros(0, dtype=np.float32)
    progress = np.linspace(0.0, 1.0, frame_count, endpoint=False)
    envelope = np.exp(-decay_rate * progress)
    attack_frames = min(
        frame_count,
        max(1, round(attack_seconds * SAMPLE_RATE)),
    )
    envelope[:attack_frames] *= np.linspace(
        0.0,
        1.0,
        attack_frames,
        endpoint=False,
    )
    release_frames = min(
        frame_count,
        max(1, round(release_seconds * SAMPLE_RATE)),
    )
    envelope[-release_frames:] *= np.linspace(
        1.0,
        0.0,
        release_frames,
        endpoint=True,
    )
    return envelope.astype(np.float32)


def filtered_noise(
    frame_count: int,
    rng: np.random.Generator,
    smoothing: int = 8,
    high_pass: bool = False,
) -> np.ndarray:
    noise = rng.standard_normal(frame_count).astype(np.float32)
    smoothing = max(1, smoothing)
    if smoothing == 1:
        return noise
    kernel = np.ones(smoothing, dtype=np.float32) / smoothing
    low = np.convolve(noise, kernel, mode="same").astype(np.float32)
    return noise - low if high_pass else low


def equal_power_pan(pan: float) -> tuple[float, float]:
    angle = (max(-1.0, min(1.0, pan)) + 1.0) * math.pi / 4.0
    return math.cos(angle), math.sin(angle)


def add_signal(
    buffer: np.ndarray,
    signal: np.ndarray,
    start_frame: int,
    amplitude: float = 1.0,
    pan: float = 0.0,
    wrap: bool = False,
) -> None:
    if signal.size == 0:
        return
    total_frames = buffer.shape[0]
    if total_frames <= 0:
        return
    signal = signal.astype(np.float32, copy=False) * amplitude
    if wrap:
        start_frame %= total_frames
    elif start_frame < 0:
        signal = signal[-start_frame:]
        start_frame = 0
        if signal.size == 0:
            return

    if buffer.ndim == 1:
        channels = [signal]
    else:
        left, right = equal_power_pan(pan)
        channels = [signal * left, signal * right]

    for channel_index, channel_signal in enumerate(channels):
        target = buffer if buffer.ndim == 1 else buffer[:, channel_index]
        if not wrap:
            if start_frame >= total_frames:
                continue
            usable = min(channel_signal.shape[0], total_frames - start_frame)
            target[start_frame : start_frame + usable] += channel_signal[:usable]
            continue

        source_offset = 0
        target_offset = start_frame
        remaining = channel_signal.shape[0]
        while remaining > 0:
            writable = min(remaining, total_frames - target_offset)
            target[target_offset : target_offset + writable] += (
                channel_signal[source_offset : source_offset + writable]
            )
            source_offset += writable
            remaining -= writable
            target_offset = 0


def tone_voice(
    frequency: float,
    duration: float,
    waveform: str = "sine",
    attack: float = 0.002,
    release: float = 0.03,
    decay: float = 3.0,
    end_frequency: float | None = None,
) -> np.ndarray:
    signal = oscillator(
        frequency,
        duration,
        waveform,
        end_frequency=end_frequency,
    )
    return signal * shaped_envelope(
        signal.shape[0],
        attack,
        release,
        decay,
    )


def bell_voice(
    frequency: float,
    duration: float,
    brightness: float = 1.0,
) -> np.ndarray:
    partials = (1.0, 2.02, 2.97, 4.13, 5.37)
    weights = (1.0, 0.54, 0.34, 0.20, 0.12)
    frame_count = max(1, round(duration * SAMPLE_RATE))
    result = np.zeros(frame_count, dtype=np.float32)
    for index, (partial, weight) in enumerate(zip(partials, weights)):
        component = oscillator(frequency * partial, duration)
        component *= shaped_envelope(
            frame_count,
            0.001 + index * 0.0004,
            0.025,
            2.2 + index * 1.1,
        )
        result += component * weight * brightness
    return result / max(1.0, float(np.sum(weights)))


def glass_voice(frequency: float, duration: float) -> np.ndarray:
    partials = (1.0, 2.01, 3.93, 6.17)
    weights = (1.0, 0.40, 0.22, 0.09)
    frame_count = max(1, round(duration * SAMPLE_RATE))
    result = np.zeros(frame_count, dtype=np.float32)
    for index, (partial, weight) in enumerate(zip(partials, weights)):
        result += (
            oscillator(frequency * partial, duration)
            * shaped_envelope(
                frame_count,
                0.012,
                0.04,
                2.0 + index * 0.9,
            )
            * weight
        )
    return result / max(1.0, float(np.sum(weights)))


def wood_hit(
    duration: float,
    rng: np.random.Generator,
    base_frequency: float = 110.0,
    weight: float = 1.0,
) -> np.ndarray:
    frame_count = max(1, round(duration * SAMPLE_RATE))
    body = oscillator(
        base_frequency * 1.25,
        duration,
        "sine",
        end_frequency=base_frequency * 0.72,
    )
    cavity = oscillator(base_frequency * 2.7, duration, "triangle")
    transient = filtered_noise(
        frame_count,
        rng,
        smoothing=5,
        high_pass=True,
    )
    body *= shaped_envelope(frame_count, 0.001, 0.04, 4.2)
    cavity *= shaped_envelope(frame_count, 0.001, 0.03, 6.5)
    transient *= shaped_envelope(frame_count, 0.0005, 0.015, 13.0)
    return (0.64 * body + 0.20 * cavity + 0.16 * transient) * weight


def air_voice(
    duration: float,
    rng: np.random.Generator,
    rising: bool,
) -> np.ndarray:
    frame_count = max(1, round(duration * SAMPLE_RATE))
    noise = filtered_noise(
        frame_count,
        rng,
        smoothing=12 if rising else 7,
        high_pass=True,
    )
    progress = np.linspace(0.0, 1.0, frame_count, endpoint=False)
    arch = np.sin(math.pi * progress) ** 1.6
    tilt = (0.25 + 0.75 * progress) if rising else (1.0 - 0.65 * progress)
    return (noise * arch * tilt).astype(np.float32)


def add_tone(
    buffer: np.ndarray,
    start: float,
    duration: float,
    frequency: float,
    amplitude: float,
    waveform: str = "sine",
    pan: float = 0.0,
    end_frequency: float | None = None,
    wrap: bool = False,
) -> None:
    voice = tone_voice(
        frequency,
        duration,
        waveform,
        end_frequency=end_frequency,
    )
    add_signal(
        buffer,
        voice,
        round(start * SAMPLE_RATE),
        amplitude,
        pan,
        wrap,
    )


def add_bell(
    buffer: np.ndarray,
    start: float,
    duration: float,
    frequency: float,
    amplitude: float,
    pan: float = 0.0,
    glass: bool = False,
    wrap: bool = False,
) -> None:
    voice = (
        glass_voice(frequency, duration)
        if glass
        else bell_voice(frequency, duration)
    )
    add_signal(
        buffer,
        voice,
        round(start * SAMPLE_RATE),
        amplitude,
        pan,
        wrap,
    )


def add_wood(
    buffer: np.ndarray,
    start: float,
    duration: float,
    rng: np.random.Generator,
    frequency: float,
    amplitude: float,
    pan: float = 0.0,
    wrap: bool = False,
) -> None:
    voice = wood_hit(duration, rng, frequency)
    add_signal(
        buffer,
        voice,
        round(start * SAMPLE_RATE),
        amplitude,
        pan,
        wrap,
    )


def add_air(
    buffer: np.ndarray,
    start: float,
    duration: float,
    rng: np.random.Generator,
    amplitude: float,
    pan: float = 0.0,
    rising: bool = True,
    wrap: bool = False,
) -> None:
    voice = air_voice(duration, rng, rising)
    add_signal(
        buffer,
        voice,
        round(start * SAMPLE_RATE),
        amplitude,
        pan,
        wrap,
    )


def render_sfx(cue_id: str, variant: int, spec: SfxSpec) -> np.ndarray:
    rng = np.random.default_rng(stable_seed(cue_id, variant))
    frame_count = round(spec.duration * SAMPLE_RATE)
    channels = 2 if spec.stereo else 1
    buffer = np.zeros(
        (frame_count, channels) if channels > 1 else frame_count,
        dtype=np.float32,
    )
    detune = 1.0 + (variant - (spec.variants + 1) / 2.0) * 0.006
    pan = (variant - (spec.variants + 1) / 2.0) * 0.035

    if cue_id == "ui_click":
        add_wood(buffer, 0.0, 0.09, rng, 740 * detune, 0.68)
        add_tone(buffer, 0.008, 0.07, 1240 * detune, 0.16)
    elif cue_id == "ui_confirm":
        add_tone(buffer, 0.0, 0.14, midi_frequency(74) * detune, 0.46, "triangle")
        add_tone(buffer, 0.075, 0.16, midi_frequency(81) * detune, 0.42, "triangle")
        add_wood(buffer, 0.0, 0.11, rng, 310, 0.24)
    elif cue_id == "ui_cancel":
        add_tone(buffer, 0.0, 0.13, midi_frequency(69) * detune, 0.42, "triangle")
        add_tone(buffer, 0.065, 0.13, midi_frequency(62) * detune, 0.38, "triangle")
        add_air(buffer, 0.0, 0.19, rng, 0.11, rising=False)
    elif cue_id == "ui_error":
        add_tone(buffer, 0.0, 0.25, midi_frequency(48) * detune, 0.48, "soft_square")
        add_tone(buffer, 0.035, 0.25, midi_frequency(49) / detune, 0.34, "triangle")
        add_wood(buffer, 0.0, 0.18, rng, 82, 0.26)
    elif cue_id == "shop_refresh":
        add_air(buffer, 0.0, 0.46, rng, 0.40, -0.75, True)
        add_air(buffer, 0.035, 0.42, rng, 0.34, 0.75, True)
        for index, note in enumerate((74, 78, 81)):
            add_tone(
                buffer,
                0.07 + index * 0.10,
                0.13,
                midi_frequency(note) * detune,
                0.18,
                "triangle",
                -0.55 + index * 0.55,
            )
    elif cue_id == "shop_buy":
        add_bell(buffer, 0.0, 0.24, midi_frequency(74) * detune, 0.44)
        add_bell(buffer, 0.055, 0.24, midi_frequency(81) * detune, 0.32)
        add_wood(buffer, 0.16, 0.17, rng, 145, 0.46)
    elif cue_id == "shop_sell":
        add_bell(buffer, 0.0, 0.25, midi_frequency(79) * detune, 0.34)
        add_bell(buffer, 0.075, 0.24, midi_frequency(72) * detune, 0.40)
        add_air(buffer, 0.04, 0.31, rng, 0.18, rising=False)
    elif cue_id == "shop_play":
        add_wood(buffer, 0.0, 0.35, rng, 105 * detune, 0.68)
        add_tone(buffer, 0.015, 0.31, midi_frequency(50), 0.24, "triangle")
        add_bell(buffer, 0.18, 0.22, midi_frequency(62), 0.20)
    elif cue_id == "shop_spell":
        add_air(buffer, 0.0, 0.61, rng, 0.40, rising=True)
        add_bell(buffer, 0.22, 0.38, midi_frequency(81) * detune, 0.38, glass=True)
        add_bell(buffer, 0.31, 0.30, midi_frequency(86) * detune, 0.24, glass=True)
    elif cue_id == "shop_triple":
        for index, note in enumerate((62, 65, 69)):
            add_bell(buffer, index * 0.16, 0.48, midi_frequency(note), 0.38)
        add_bell(buffer, 0.50, 0.62, midi_frequency(74), 0.48, glass=True)
        add_wood(buffer, 0.0, 0.36, rng, 92, 0.32)
    elif cue_id == "shop_discover_open":
        add_air(buffer, 0.0, 0.76, rng, 0.34, -0.75, True)
        add_air(buffer, 0.0, 0.76, rng, 0.34, 0.75, True)
        for index, note in enumerate((74, 81, 86)):
            add_bell(
                buffer,
                0.18 + index * 0.13,
                0.42,
                midi_frequency(note),
                0.24,
                -0.6 + index * 0.6,
                glass=True,
            )
    elif cue_id == "shop_discover_pick":
        add_air(buffer, 0.0, 0.32, rng, 0.17, rising=False)
        add_tone(buffer, 0.02, 0.20, midi_frequency(69) * detune, 0.30, "triangle")
        add_tone(buffer, 0.11, 0.23, midi_frequency(74) * detune, 0.38, "triangle")
        add_wood(buffer, 0.0, 0.16, rng, 260, 0.25)
    elif cue_id == "shop_upgrade":
        for index, note in enumerate((62, 65, 69, 74)):
            add_bell(buffer, index * 0.15, 0.45, midi_frequency(note), 0.34)
            add_wood(buffer, index * 0.15, 0.14, rng, 180 + index * 35, 0.18)
        add_tone(buffer, 0.55, 0.50, midi_frequency(50), 0.24, "triangle")
    elif cue_id == "battle_attack_light":
        add_air(buffer, 0.0, 0.265, rng, 0.56, pan, True)
        add_tone(
            buffer,
            0.025,
            0.22,
            900 * detune,
            0.16,
            "triangle",
            end_frequency=370 * detune,
        )
    elif cue_id == "battle_hit":
        add_wood(buffer, 0.0, 0.28, rng, 105 * detune, 0.86)
        add_tone(
            buffer,
            0.0,
            0.27,
            118 * detune,
            0.55,
            end_frequency=54 * detune,
        )
    elif cue_id == "battle_shield_gain":
        add_bell(buffer, 0.02, 0.44, midi_frequency(76) * detune, 0.38, glass=True)
        add_bell(buffer, 0.12, 0.35, midi_frequency(83) * detune, 0.32, glass=True)
        add_air(buffer, 0.0, 0.46, rng, 0.18, rising=True)
    elif cue_id == "battle_shield_break":
        add_air(buffer, 0.0, 0.51, rng, 0.40, rising=False)
        for index, note in enumerate((90, 86, 81, 78)):
            add_bell(
                buffer,
                index * 0.045,
                0.30,
                midi_frequency(note) * detune,
                0.24,
                glass=True,
            )
    elif cue_id == "battle_stat_up":
        for index, note in enumerate((74, 77, 81)):
            add_tone(
                buffer,
                index * 0.075,
                0.24,
                midi_frequency(note) * detune,
                0.34,
                "triangle",
            )
    elif cue_id == "battle_death":
        add_wood(buffer, 0.0, 0.70, rng, 72 * detune, 0.90)
        add_tone(
            buffer,
            0.0,
            0.78,
            92 * detune,
            0.52,
            "soft_square",
            end_frequency=43 * detune,
        )
        add_air(buffer, 0.17, 0.69, rng, 0.24, rising=False)
    elif cue_id == "battle_token_death":
        add_wood(buffer, 0.0, 0.25, rng, 420 * detune, 0.55)
        add_air(buffer, 0.015, 0.31, rng, 0.28, rising=False)
        add_tone(buffer, 0.02, 0.20, 1100 * detune, 0.12, "triangle")
    elif cue_id == "battle_summon":
        add_air(buffer, 0.0, 0.52, rng, 0.44, rising=True)
        add_bell(buffer, 0.26, 0.27, midi_frequency(74) * detune, 0.28, glass=True)
        add_wood(buffer, 0.37, 0.16, rng, 165, 0.26)
    elif cue_id == "battle_victory":
        for index, note in enumerate((62, 66, 69, 74)):
            add_bell(buffer, index * 0.18, 0.62, midi_frequency(note), 0.42)
        add_tone(buffer, 0.58, 0.70, midi_frequency(50), 0.22, "triangle")
    elif cue_id == "battle_defeat":
        for index, note in enumerate((62, 60, 58, 57)):
            add_bell(buffer, index * 0.18, 0.58, midi_frequency(note), 0.34)
        add_tone(
            buffer,
            0.42,
            0.88,
            midi_frequency(38),
            0.36,
            "soft_square",
            end_frequency=midi_frequency(33),
        )
    elif cue_id == "run_node_select":
        add_wood(buffer, 0.0, 0.28, rng, 190 * detune, 0.62)
        add_bell(buffer, 0.075, 0.23, midi_frequency(74) * detune, 0.24)
    elif cue_id == "run_reward":
        add_air(buffer, 0.0, 0.78, rng, 0.20, rising=True)
        for index, note in enumerate((62, 65, 69, 72)):
            add_bell(
                buffer,
                0.12 + index * 0.11,
                0.44,
                midi_frequency(note) * detune,
                0.28,
            )
    else:
        raise ValueError(f"Unsupported cue: {cue_id}")

    target_rms = -25.0 if spec.domain == "UI" else -23.0
    if spec.domain == "Battle":
        target_rms = -21.0
    if cue_id in {
        "shop_triple",
        "shop_upgrade",
        "battle_victory",
        "battle_defeat",
        "run_reward",
    }:
        target_rms = -19.0
    return finalize_audio(buffer, target_rms, close_loop=False)


def pluck_voice(
    frequency: float,
    duration: float,
    brightness: float = 1.0,
) -> np.ndarray:
    base = oscillator(frequency, duration, "triangle")
    octave = oscillator(frequency * 2.0, duration, "sine")
    fifth = oscillator(frequency * 1.5, duration, "sine")
    frame_count = base.shape[0]
    envelope = shaped_envelope(frame_count, 0.002, 0.06, 4.2)
    return (
        base * 0.62 + octave * 0.24 * brightness + fifth * 0.14
    ) * envelope


def add_pluck_looped(
    buffer: np.ndarray,
    start_frame: int,
    duration: float,
    midi_note: float,
    amplitude: float,
    pan: float,
) -> None:
    voice = pluck_voice(midi_frequency(midi_note), duration)
    add_signal(buffer, voice, start_frame, amplitude, pan, wrap=True)


def add_circular_reverb(buffer: np.ndarray, strength: float) -> None:
    dry = buffer.copy()
    taps = (
        (0.029, 0.16, -0.35),
        (0.043, 0.13, 0.35),
        (0.071, 0.10, -0.20),
        (0.113, 0.08, 0.20),
        (0.173, 0.06, -0.10),
        (0.257, 0.045, 0.10),
    )
    for delay_seconds, gain, cross in taps:
        delayed = np.roll(dry, round(delay_seconds * SAMPLE_RATE), axis=0)
        buffer[:, 0] += (
            delayed[:, 0] * gain + delayed[:, 1] * gain * cross
        ) * strength
        buffer[:, 1] += (
            delayed[:, 1] * gain + delayed[:, 0] * gain * cross
        ) * strength


def add_periodic_drone(
    buffer: np.ndarray,
    midi_note: float,
    amplitude: float,
    pan: float,
) -> None:
    duration = buffer.shape[0] / SAMPLE_RATE
    desired_frequency = midi_frequency(midi_note)
    cycle_count = max(1, round(desired_frequency * duration))
    frequency = cycle_count / duration
    time = np.arange(buffer.shape[0], dtype=np.float64) / SAMPLE_RATE
    signal = (
        np.sin(2.0 * math.pi * frequency * time)
        + 0.28 * np.sin(2.0 * math.pi * frequency * 2.0 * time)
    ).astype(np.float32)
    left, right = equal_power_pan(pan)
    buffer[:, 0] += signal * amplitude * left
    buffer[:, 1] += signal * amplitude * right


def render_bgm(cue_id: str, spec: BgmSpec) -> np.ndarray:
    rng = np.random.default_rng(stable_seed(cue_id))
    buffer = np.zeros((spec.sample_count, 2), dtype=np.float32)
    beat_frames = spec.samples_per_beat
    beat_seconds = beat_frames / SAMPLE_RATE

    if cue_id == "bgm_main_menu":
        chords = (
            (50, 53, 57),
            (55, 59, 62),
            (48, 52, 55),
            (45, 50, 52),
        )
        motif = (62, 65, 67, 69, 72, 69, 67, 65)
        add_periodic_drone(buffer, 38, 0.055, -0.25)
        add_periodic_drone(buffer, 45, 0.040, 0.25)
        for bar in range(spec.bars):
            chord = chords[(bar // 2) % len(chords)]
            bar_start = bar * 4 * beat_frames
            for chord_index, note in enumerate(chord):
                add_pluck_looped(
                    buffer,
                    bar_start + chord_index * beat_frames // 12,
                    beat_seconds * 1.8,
                    note + 12,
                    0.095,
                    -0.46 + chord_index * 0.46,
                )
            bass = pluck_voice(
                midi_frequency(chord[0] - 12),
                beat_seconds * 2.4,
                0.5,
            )
            add_signal(buffer, bass, bar_start, 0.15, -0.18, wrap=True)
            add_wood(
                buffer,
                bar_start / SAMPLE_RATE,
                0.24,
                rng,
                94,
                0.07,
                -0.2,
                True,
            )
            if bar % 2 == 1:
                note = motif[(bar // 2) % len(motif)]
                add_bell(
                    buffer,
                    (bar_start + 2 * beat_frames) / SAMPLE_RATE,
                    beat_seconds * 1.4,
                    midi_frequency(note),
                    0.055,
                    0.28,
                    False,
                    True,
                )
    elif cue_id == "bgm_run_shop":
        chords = (
            (43, 46, 50),
            (48, 51, 55),
            (50, 53, 57),
            (46, 50, 53),
        )
        scale = (55, 57, 58, 60, 62, 64, 65, 67)
        add_periodic_drone(buffer, 43, 0.050, -0.28)
        add_periodic_drone(buffer, 50, 0.035, 0.28)
        for bar in range(spec.bars):
            chord = chords[(bar // 2) % len(chords)]
            bar_start = bar * 4 * beat_frames
            for beat in range(4):
                note = chord[0] - 12 if beat in (0, 2) else chord[1] - 12
                add_pluck_looped(
                    buffer,
                    bar_start + beat * beat_frames,
                    beat_seconds * 0.86,
                    note,
                    0.14,
                    -0.2 if beat % 2 == 0 else 0.2,
                )
                add_wood(
                    buffer,
                    (bar_start + beat * beat_frames) / SAMPLE_RATE,
                    0.16,
                    rng,
                    118 if beat in (0, 2) else 210,
                    0.075 if beat in (0, 2) else 0.048,
                    -0.30 if beat % 2 == 0 else 0.30,
                    True,
                )
            for eighth in range(8):
                note = scale[(bar * 3 + eighth * 2) % len(scale)]
                add_pluck_looped(
                    buffer,
                    bar_start + eighth * beat_frames // 2,
                    beat_seconds * 0.46,
                    note,
                    0.055 if eighth % 2 else 0.075,
                    -0.4 + 0.8 * ((eighth % 4) / 3.0),
                )
            if bar % 4 == 3:
                add_air(
                    buffer,
                    (bar_start + 3 * beat_frames) / SAMPLE_RATE,
                    beat_seconds * 0.85,
                    rng,
                    0.035,
                    0.0,
                    False,
                    True,
                )
    elif cue_id == "bgm_battle_normal":
        progression = (38, 41, 36, 33)
        ostinato = (50, 57, 53, 57, 50, 58, 53, 57)
        add_periodic_drone(buffer, 38, 0.050, -0.2)
        add_periodic_drone(buffer, 45, 0.025, 0.2)
        for bar in range(spec.bars):
            root = progression[(bar // 4) % len(progression)]
            bar_start = bar * 4 * beat_frames
            for beat in range(4):
                beat_start = bar_start + beat * beat_frames
                drum = wood_hit(
                    beat_seconds * 0.72,
                    rng,
                    74 if beat in (0, 2) else 132,
                )
                add_signal(
                    buffer,
                    drum,
                    beat_start,
                    0.14 if beat in (0, 2) else 0.075,
                    -0.18 if beat % 2 == 0 else 0.18,
                    True,
                )
                add_pluck_looped(
                    buffer,
                    beat_start,
                    beat_seconds * 0.82,
                    root,
                    0.14,
                    -0.22 if beat % 2 == 0 else 0.22,
                )
            for eighth in range(8):
                note = ostinato[(eighth + bar) % len(ostinato)]
                add_pluck_looped(
                    buffer,
                    bar_start + eighth * beat_frames // 2,
                    beat_seconds * 0.42,
                    note,
                    0.070 if eighth % 2 == 0 else 0.050,
                    -0.48 + 0.96 * (eighth / 7.0),
                )
            if bar % 4 == 3:
                add_bell(
                    buffer,
                    (bar_start + 3 * beat_frames) / SAMPLE_RATE,
                    beat_seconds * 1.2,
                    midi_frequency(root + 24),
                    0.032,
                    0.15,
                    False,
                    True,
                )
    else:
        raise ValueError(f"Unsupported BGM cue: {cue_id}")

    add_circular_reverb(
        buffer,
        0.72 if cue_id == "bgm_main_menu" else 0.52,
    )
    return finalize_audio(buffer, spec.target_rms_db, close_loop=True)


def finalize_audio(
    audio: np.ndarray,
    target_rms_db: float,
    close_loop: bool,
) -> np.ndarray:
    result = np.asarray(audio, dtype=np.float32)
    if result.ndim == 1:
        result = result - np.mean(result, dtype=np.float64)
    else:
        result = result - np.mean(result, axis=0, dtype=np.float64)

    peak = float(np.max(np.abs(result))) if result.size else 0.0
    rms = (
        float(np.sqrt(np.mean(np.square(result), dtype=np.float64)))
        if result.size
        else 0.0
    )
    target_rms = 10.0 ** (target_rms_db / 20.0)
    peak_limit = 10.0 ** (PEAK_LIMIT_DB / 20.0)
    if rms > 1e-9:
        scale = target_rms / rms
        if peak * scale > peak_limit:
            scale = peak_limit / max(peak, 1e-9)
        result = result * scale

    if close_loop:
        seam_frames = min(512, result.shape[0])
        ramp = np.linspace(0.0, 1.0, seam_frames, dtype=np.float32)
        ramp = ramp * ramp * (3.0 - 2.0 * ramp)
        delta = result[0] - result[-1]
        if result.ndim == 1:
            result[-seam_frames:] += ramp * delta
        else:
            result[-seam_frames:] += ramp[:, None] * delta[None, :]
    else:
        release_frames = min(
            result.shape[0],
            max(1, round(0.012 * SAMPLE_RATE)),
        )
        fade = np.linspace(
            1.0,
            0.0,
            release_frames,
            dtype=np.float32,
        )
        if result.ndim == 1:
            result[-release_frames:] *= fade
        else:
            result[-release_frames:] *= fade[:, None]

    return np.clip(result, -1.0, 1.0).astype(np.float32)


def audio_metrics(audio: np.ndarray) -> dict[str, float | int]:
    peak = float(np.max(np.abs(audio))) if audio.size else 0.0
    rms = (
        float(np.sqrt(np.mean(np.square(audio), dtype=np.float64)))
        if audio.size
        else 0.0
    )
    if audio.ndim == 1:
        seam = float(abs(float(audio[0]) - float(audio[-1])))
        channels = 1
    else:
        seam = float(np.max(np.abs(audio[0] - audio[-1])))
        channels = audio.shape[1]
    return {
        "channels": channels,
        "sampleCount": int(audio.shape[0]),
        "durationSeconds": round(audio.shape[0] / SAMPLE_RATE, 6),
        "samplePeakDbfs": round(20.0 * math.log10(max(peak, 1e-12)), 4),
        "rmsDbfs": round(20.0 * math.log10(max(rms, 1e-12)), 4),
        "seamDelta": round(seam, 8),
    }


def write_pcm24(
    path: Path,
    audio: np.ndarray,
    dither_seed: int,
) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    rng = np.random.default_rng(dither_seed)
    channels = 1 if audio.ndim == 1 else audio.shape[1]
    with wave.open(str(temporary), "wb") as output:
        output.setnchannels(channels)
        output.setsampwidth(3)
        output.setframerate(SAMPLE_RATE)
        chunk_frames = 65_536
        for start in range(0, audio.shape[0], chunk_frames):
            chunk = audio[start : start + chunk_frames]
            dither = (
                rng.random(chunk.shape, dtype=np.float32)
                - rng.random(chunk.shape, dtype=np.float32)
            ) / 8_388_608.0
            integers = np.rint(
                np.clip(chunk + dither, -1.0, 1.0) * 8_388_607.0
            ).astype(np.int32)
            unsigned = (integers.reshape(-1) & 0xFFFFFF).astype(np.uint32)
            packed = np.empty((unsigned.shape[0], 3), dtype=np.uint8)
            packed[:, 0] = unsigned & 0xFF
            packed[:, 1] = (unsigned >> 8) & 0xFF
            packed[:, 2] = (unsigned >> 16) & 0xFF
            output.writeframesraw(packed.tobytes())
    temporary.replace(path)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def generate(output_root: Path) -> dict[str, object]:
    entries: list[dict[str, object]] = []
    for cue_id, spec in BGM_SPECS.items():
        relative = (
            Path("Music")
            / "Placeholder"
            / f"placeholder_{cue_id}_v01.wav"
        )
        path = output_root / relative
        print(f"[BGM] {cue_id} -> {relative}")
        audio = render_bgm(cue_id, spec)
        write_pcm24(path, audio, stable_seed(cue_id, 99))
        metrics = audio_metrics(audio)
        entries.append(
            {
                "cueId": cue_id,
                "variant": 1,
                "bus": "Music",
                "assetStatus": "Placeholder",
                "relativePath": relative.as_posix(),
                "loopStartSample": 0,
                "loopEndSample": metrics["sampleCount"],
                "mode": spec.mode,
                "bpm": spec.bpm,
                **metrics,
                "sha256": sha256_file(path),
            }
        )

    for cue_id, spec in SFX_SPECS.items():
        for variant in range(1, spec.variants + 1):
            relative = (
                Path("SFX")
                / spec.domain
                / "Placeholder"
                / f"placeholder_sfx_{cue_id}_{variant:02d}.wav"
            )
            path = output_root / relative
            print(f"[SFX] {cue_id} {variant:02d} -> {relative}")
            audio = render_sfx(cue_id, variant, spec)
            write_pcm24(
                path,
                audio,
                stable_seed(cue_id, variant + 100),
            )
            metrics = audio_metrics(audio)
            entries.append(
                {
                    "cueId": cue_id,
                    "variant": variant,
                    "bus": "UI" if spec.domain == "UI" else "SFX",
                    "assetStatus": "Placeholder",
                    "relativePath": relative.as_posix(),
                    "loopStartSample": None,
                    "loopEndSample": None,
                    **metrics,
                    "sha256": sha256_file(path),
                }
            )

    manifest = {
        "schemaVersion": 1,
        "generatorVersion": GENERATOR_VERSION,
        "masterSeed": f"0x{MASTER_SEED:016X}",
        "generationRuntime": {
            "pythonImplementation": platform.python_implementation(),
            "pythonVersion": platform.python_version(),
            "numpyVersion": np.__version__,
        },
        "generatedAssetStatus": "Local Synth Placeholder",
        "productionReady": False,
        "source": (
            "Deterministic procedural synthesis; oscillators, filtered noise, "
            "and mathematical envelopes only; no external samples."
        ),
        "sampleRate": SAMPLE_RATE,
        "bitDepth": BIT_DEPTH,
        "samplePeakCeilingDbfs": PEAK_LIMIT_DB,
        "bgmCueCount": len(BGM_SPECS),
        "sfxCueCount": len(SFX_SPECS),
        "sfxClipCount": sum(spec.variants for spec in SFX_SPECS.values()),
        "runtimeClipCount": len(entries),
        "files": entries,
    }
    manifest_path = output_root / "Placeholder" / "placeholder_audio_manifest.json"
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    return manifest


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[1]
    default_output = (
        repo_root / "sc" / "Assets" / "Audio" / "Presentation"
    )
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output-root",
        type=Path,
        default=default_output,
        help="Unity Presentation audio directory.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest = generate(args.output_root.resolve())
    print(
        "Generated "
        f"{manifest['runtimeClipCount']} placeholder clips "
        f"({manifest['bgmCueCount']} BGM + "
        f"{manifest['sfxClipCount']} SFX variants)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
