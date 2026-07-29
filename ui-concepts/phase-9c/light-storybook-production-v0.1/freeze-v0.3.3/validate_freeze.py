from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from PIL import Image


PACKAGE_DIR = Path(__file__).resolve().parent
REPO_ROOT = Path(__file__).resolve().parents[4]
DEFAULT_MANIFEST = PACKAGE_DIR / "VISUAL-BASELINES-v0.3.3.json"
DEFAULT_REPORT = PACKAGE_DIR / "FREEZE-VALIDATION-REPORT-v0.3.3.json"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def text_lf_sha256(path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    normalized = text.replace("\r\n", "\n").replace("\r", "\n")
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def repo_path(relative: str) -> Path:
    return REPO_ROOT / Path(relative)


def image_luma_stats(path: Path, size: tuple[int, int]) -> dict[str, float]:
    with Image.open(path) as source:
        image = source.convert("RGB").resize(size, Image.Resampling.LANCZOS)
    raw = image.tobytes()
    light_mid = 0
    near_black = 0
    luma_sum = 0.0
    pixel_count = size[0] * size[1]
    for index in range(0, len(raw), 3):
        luma = (
            0.2126 * raw[index]
            + 0.7152 * raw[index + 1]
            + 0.0722 * raw[index + 2]
        )
        luma_sum += luma
        light_mid += luma >= 85
        near_black += luma < 25
    return {
        "meanLuma": round(luma_sum / pixel_count, 2),
        "lightMidRatio": round(light_mid / pixel_count, 6),
        "nearBlackRatio": round(near_black / pixel_count, 6),
    }


class Validator:
    def __init__(self, manifest: dict[str, Any]) -> None:
        self.manifest = manifest
        self.checks: list[dict[str, Any]] = []

    def add(self, check_id: str, passed: bool, details: Any) -> None:
        self.checks.append(
            {
                "id": check_id,
                "status": "pass" if passed else "fail",
                "details": details,
            }
        )

    def validate_hashes(self) -> None:
        entries = (
            self.manifest["frozenDocuments"]
            + self.manifest["immutableFiles"]
        )
        results = []
        passed = True
        for entry in entries:
            path = repo_path(entry["path"])
            exists = path.is_file()
            if not exists:
                actual = None
            elif entry.get("hashMode") == "text-lf":
                actual = text_lf_sha256(path)
            else:
                actual = sha256(path)
            match = exists and actual == entry["sha256"]
            results.append(
                {
                    "path": entry["path"],
                    "exists": exists,
                    "hashMatches": match,
                    "actualSha256": actual,
                }
            )
            passed &= match
        self.add("frozen-file-hashes", passed, results)

    def validate_images(self) -> None:
        results = []
        passed = True
        for expected in self.manifest["imageExpectations"]:
            path = repo_path(expected["path"])
            if not path.is_file():
                results.append({"path": expected["path"], "exists": False})
                passed = False
                continue
            with Image.open(path) as image:
                actual = {
                    "width": image.width,
                    "height": image.height,
                    "mode": image.mode,
                }
            match = all(actual[key] == expected[key] for key in actual)
            results.append(
                {
                    "path": expected["path"],
                    "exists": True,
                    "matches": match,
                    "actual": actual,
                }
            )
            passed &= match
        self.add("baseline-image-geometry", passed, results)

    def validate_formal_art(self) -> None:
        formal = self.manifest["formalArt"]
        gate = self.manifest["brightnessGate"]
        spec = load_json(repo_path(formal["specPath"]))
        cards = spec.get("cards", [])
        expected_hashes = formal["sha256ById"]
        ids = {card.get("id") for card in cards}
        identity_ok = (
            len(cards) == formal["expectedCount"]
            and ids == set(expected_hashes)
        )
        results = []
        passed = identity_ok
        for card in cards:
            path = repo_path(f"{formal['root']}/{card['artFile']}")
            exists = path.is_file()
            if not exists:
                results.append({"id": card["id"], "exists": False})
                passed = False
                continue
            with Image.open(path) as image:
                width, height = image.size
            ratio = width / height
            stats = image_luma_stats(path, tuple(gate["resize"]))
            hash_matches = sha256(path) == expected_hashes[card["id"]]
            ratio_ok = (
                formal["aspectRatioMin"]
                <= ratio
                <= formal["aspectRatioMax"]
            )
            brightness_ok = (
                stats["lightMidRatio"] >= gate["lightMidRatioMin"]
                and stats["nearBlackRatio"]
                < gate["nearBlackRatioMaxExclusive"]
            )
            card_ok = hash_matches and ratio_ok and brightness_ok
            passed &= card_ok
            results.append(
                {
                    "id": card["id"],
                    "name": card["name"],
                    "exists": True,
                    "hashMatches": hash_matches,
                    "size": [width, height],
                    "aspectRatio": round(ratio, 6),
                    "brightness": stats,
                    "passes": card_ok,
                }
            )
        self.add(
            "formal-art-count-hash-aspect-brightness",
            passed,
            {
                "identitySetMatches": identity_ok,
                "expectedCount": formal["expectedCount"],
                "actualCount": len(cards),
                "cards": results,
            },
        )

    def validate_prompt(self) -> None:
        contract = self.manifest["promptContract"]
        text = repo_path(contract["path"]).read_text(encoding="utf-8")
        folded = text.casefold()
        forbidden_hits = [
            phrase
            for phrase in contract["forbiddenPhrases"]
            if phrase.casefold() in folded
        ]
        missing_required = [
            phrase
            for phrase in contract["requiredPhrases"]
            if phrase not in text
        ]
        self.add(
            "production-prompt-contract",
            not forbidden_hits and not missing_required,
            {
                "forbiddenHits": forbidden_hits,
                "missingRequiredPhrases": missing_required,
            },
        )

    def validate_source_contracts(self) -> None:
        results = []
        passed = True
        for contract in self.manifest["sourceContracts"]:
            path = repo_path(contract["path"])
            exists = path.is_file()
            text = path.read_text(encoding="utf-8") if exists else ""
            missing = [
                token
                for token in contract["requiredTokens"]
                if token not in text
            ]
            contract_ok = exists and not missing
            passed &= contract_ok
            results.append(
                {
                    "id": contract["id"],
                    "path": contract["path"],
                    "exists": exists,
                    "missingTokens": missing,
                }
            )
        self.add("source-structure-contracts", passed, results)

    def validate_catalog_bindings(self) -> None:
        bindings = self.manifest["catalogBindings"]
        asset_text = repo_path(bindings["path"]).read_text(encoding="utf-8")
        meta_text = repo_path(bindings["shieldMetaPath"]).read_text(
            encoding="utf-8"
        )
        card_pattern = (
            r"battleShieldOverlay:\s*\{[^\r\n]*guid:\s*"
            + re.escape(bindings["cardShieldGuid"])
        )
        standee_pattern = (
            r"battleStandeeShieldOverlay:\s*\{[^\r\n]*guid:\s*"
            + re.escape(bindings["standeeShieldGuid"])
        )
        card_ok = re.search(card_pattern, asset_text) is not None
        standee_ok = re.search(standee_pattern, asset_text) is not None
        meta_ok = (
            f"guid: {bindings['standeeShieldGuid']}" in meta_text
        )
        self.add(
            "catalog-shield-bindings",
            card_ok and standee_ok and meta_ok,
            {
                "cardShieldPreserved": card_ok,
                "standeeShieldSeparated": standee_ok,
                "standeeMetaGuidMatches": meta_ok,
            },
        )

    def validate_shield_alpha(self) -> None:
        gate = self.manifest["shieldAlphaGate"]
        path = repo_path(gate["path"])
        with Image.open(path) as source:
            image = source.convert("RGBA")
        alpha = image.getchannel("A")
        width, height = image.size
        left, top, right, bottom = gate["centerBox"]
        center = alpha.crop(
            (
                int(left * width),
                int(top * height),
                int(right * width),
                int(bottom * height),
            )
        )
        center_raw = center.tobytes()
        alpha_raw = alpha.tobytes()
        center_transparent_ratio = center_raw.count(0) / len(center_raw)
        non_zero_ratio = sum(value > 0 for value in alpha_raw) / len(alpha_raw)
        corners = [
            alpha.getpixel((0, 0)),
            alpha.getpixel((width - 1, 0)),
            alpha.getpixel((0, height - 1)),
            alpha.getpixel((width - 1, height - 1)),
        ]
        passed = (
            center_transparent_ratio
            >= gate["centerTransparentRatioMin"]
            and max(corners) <= gate["cornerAlphaMax"]
            and gate["nonZeroAlphaRatioMin"]
            <= non_zero_ratio
            <= gate["nonZeroAlphaRatioMax"]
        )
        self.add(
            "standee-shield-alpha",
            passed,
            {
                "size": [width, height],
                "centerTransparentRatio": round(
                    center_transparent_ratio, 6
                ),
                "cornerAlpha": corners,
                "nonZeroAlphaRatio": round(non_zero_ratio, 6),
            },
        )

    def report(self) -> dict[str, Any]:
        passed = all(check["status"] == "pass" for check in self.checks)
        return {
            "version": self.manifest["version"],
            "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
            "result": (
                "PASS_OFFLINE_UNITY_PENDING"
                if passed
                else "FAIL_OFFLINE"
            ),
            "summary": {
                "passed": sum(
                    check["status"] == "pass" for check in self.checks
                ),
                "failed": sum(
                    check["status"] == "fail" for check in self.checks
                ),
                "unityStatus": self.manifest["status"]["unity"],
            },
            "checks": self.checks,
            "manualGates": self.manifest["manualGates"],
            "unityGates": self.manifest["unityGates"],
        }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate the Light Storybook v0.3.3 offline freeze."
    )
    parser.add_argument(
        "--manifest",
        type=Path,
        default=DEFAULT_MANIFEST,
    )
    parser.add_argument(
        "--report",
        type=Path,
        default=DEFAULT_REPORT,
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest = load_json(args.manifest.resolve())
    validator = Validator(manifest)
    validator.validate_hashes()
    validator.validate_images()
    validator.validate_formal_art()
    validator.validate_prompt()
    validator.validate_source_contracts()
    validator.validate_catalog_bindings()
    validator.validate_shield_alpha()
    report = validator.report()
    args.report.resolve().write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(report["result"])
    print(
        f"checks: {report['summary']['passed']} passed, "
        f"{report['summary']['failed']} failed"
    )
    print(f"report: {args.report.resolve()}")
    if report["result"] == "FAIL_OFFLINE":
        failed = [
            check["id"]
            for check in report["checks"]
            if check["status"] == "fail"
        ]
        print("failed checks: " + ", ".join(failed))
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
