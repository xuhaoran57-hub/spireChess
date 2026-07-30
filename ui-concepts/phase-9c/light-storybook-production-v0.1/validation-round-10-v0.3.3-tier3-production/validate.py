from __future__ import annotations

import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from PIL import Image


ROUND_DIR = Path(__file__).resolve().parent
PROJECT_DIR = ROUND_DIR.parent
REPO_ROOT = Path(__file__).resolve().parents[4]
MANIFEST_PATH = PROJECT_DIR / "PRODUCTION-MANIFEST-v0.3.3.json"
REPORT_PATH = ROUND_DIR / "VALIDATION-REPORT-v0.3.3.json"
CATALOG_PATH = (
    REPO_ROOT
    / "sc"
    / "Assets"
    / "Configs"
    / "Presentation"
    / "PresentationSpriteCatalog_LightStorybookProductionV033Batch03.asset"
)
BATCH_TWO_CATALOG_PATH = (
    REPO_ROOT
    / "sc"
    / "Assets"
    / "Configs"
    / "Presentation"
    / "PresentationSpriteCatalog_LightStorybookProductionV033Batch02.asset"
)
FROZEN_CATALOG_PATH = (
    REPO_ROOT
    / "sc"
    / "Assets"
    / "Configs"
    / "Presentation"
    / "PresentationSpriteCatalog_LightStorybookFormalCatalogV032.asset"
)
RUNTIME_CATALOG_PATH = (
    REPO_ROOT
    / "sc"
    / "Assets"
    / "Configs"
    / "Presentation"
    / "PresentationSpriteCatalog.asset"
)
BATCH_ID = "batch-03-tier3"
UNITY_ART_PREFIX = (
    REPO_ROOT
    / "sc"
    / "Assets"
    / "Art"
    / "Presentation"
    / "Calibration"
    / "LightStorybookProductionV033Batch03"
)
PREVIOUS_ART_IDS = {
    "placeholder_card_copper_ring_apprentice",
    "placeholder_card_hearth_core_spark",
    "placeholder_card_stardust_attendant",
    "placeholder_card_stargazing_apprentice",
    "placeholder_card_wandering_swordsman",
    "placeholder_card_rending_cub",
    "placeholder_card_moss_mark_seedling",
    "placeholder_card_ember_engraver",
    "placeholder_card_shieldbreaker_blade_blank",
    "placeholder_card_shieldwall_furnace_keeper",
    "placeholder_card_moon_phase_scribe",
    "placeholder_card_rune_ward_reader",
    "placeholder_card_star_etched_timekeeper",
    "placeholder_card_black_market_vendor",
    "placeholder_card_mercenary_shieldbearer",
    "placeholder_card_root_devourer",
    "placeholder_card_swiftwing_forest_hawk",
    "placeholder_card_two_tailed_fox_spirit",
}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def image_luma(path: Path) -> dict[str, float]:
    with Image.open(path) as source:
        image = source.convert("RGB").resize(
            (160, 128),
            Image.Resampling.LANCZOS,
        )
    raw = image.tobytes()
    count = 160 * 128
    light_mid = 0
    near_black = 0
    luma_sum = 0.0
    for index in range(0, len(raw), 3):
        value = (
            0.2126 * raw[index]
            + 0.7152 * raw[index + 1]
            + 0.0722 * raw[index + 2]
        )
        luma_sum += value
        light_mid += value >= 85
        near_black += value < 25
    return {
        "meanLuma": round(luma_sum / count, 2),
        "lightMidRatio": round(light_mid / count, 6),
        "nearBlackRatio": round(near_black / count, 6),
    }


def catalog_entries(path: Path) -> dict[str, str]:
    text = path.read_text(encoding="utf-8")
    pattern = re.compile(
        r"^  - id: (.+)\r?\n"
        r"    sprite: \{fileID: 21300000, guid: ([0-9a-f]{32}),",
        re.MULTILINE,
    )
    return dict(pattern.findall(text))


def main() -> int:
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    items = [
        item
        for item in manifest["items"]
        if item["batchId"] == BATCH_ID
    ]
    checks: list[dict[str, Any]] = []
    identity_passed = (
        len(items) == 7
        and all(item["kind"] == "Minion" for item in items)
        and all(item["tier"] == 3 for item in items)
        and all(item["status"] == "generated" for item in items)
    )
    checks.append(
        {
            "id": "batch-identity",
            "status": "pass" if identity_passed else "fail",
            "details": {
                "count": len(items),
                "ids": [item["id"] for item in items],
            },
        }
    )

    art_results = []
    art_passed = identity_passed
    for item in items:
        path = REPO_ROOT / item["artFile"]
        unity_path = UNITY_ART_PREFIX / path.name
        exists = path.is_file() and unity_path.is_file()
        if not exists:
            art_results.append({"id": item["id"], "exists": False})
            art_passed = False
            continue
        with Image.open(path) as image:
            width, height = image.size
        ratio = width / height
        luma = image_luma(path)
        source_hash = sha256(path)
        unity_hash = sha256(unity_path)
        passed = (
            1.23 <= ratio <= 1.27
            and luma["lightMidRatio"] >= 0.5
            and luma["nearBlackRatio"] < 0.12
            and source_hash == item["sha256"]
            and unity_hash == source_hash
        )
        art_passed &= passed
        art_results.append(
            {
                "id": item["id"],
                "exists": True,
                "size": [width, height],
                "aspectRatio": round(ratio, 6),
                "luma": luma,
                "sha256": source_hash,
                "unityCopyMatches": unity_hash == source_hash,
                "passes": passed,
            }
        )
    checks.append(
        {
            "id": "art-hash-aspect-brightness",
            "status": "pass" if art_passed else "fail",
            "details": art_results,
        }
    )

    isolated = catalog_entries(CATALOG_PATH)
    batch_two = catalog_entries(BATCH_TWO_CATALOG_PATH)
    frozen = catalog_entries(FROZEN_CATALOG_PATH)
    runtime = catalog_entries(RUNTIME_CATALOG_PATH)
    catalog_results = []
    catalog_passed = True
    for item in items:
        meta_path = UNITY_ART_PREFIX / (Path(item["artFile"]).name + ".meta")
        meta_text = (
            meta_path.read_text(encoding="utf-8")
            if meta_path.is_file()
            else ""
        )
        match = re.search(r"^guid: ([0-9a-f]{32})$", meta_text, re.MULTILINE)
        guid = match.group(1) if match else None
        passed = (
            guid is not None
            and isolated.get(item["artId"]) == guid
            and item["artId"] not in batch_two
            and item["artId"] not in frozen
            and item["artId"] not in runtime
        )
        catalog_passed &= passed
        catalog_results.append(
            {
                "id": item["id"],
                "artId": item["artId"],
                "metaGuid": guid,
                "isolatedCatalogGuid": isolated.get(item["artId"]),
                "absentFromBatchTwo": item["artId"] not in batch_two,
                "absentFromFrozenV032": item["artId"] not in frozen,
                "absentFromRuntime": item["artId"] not in runtime,
                "passes": passed,
            }
        )
    catalog_passed = (
        catalog_passed
        and len(isolated) == 60
        and PREVIOUS_ART_IDS.issubset(isolated)
    )
    checks.append(
        {
            "id": "isolated-catalog-bindings",
            "status": "pass" if catalog_passed else "fail",
            "details": {
                "isolatedCatalogEntries": len(isolated),
                "containsAllPreviousArtIds":
                    PREVIOUS_ART_IDS.issubset(isolated),
                "cards": catalog_results,
            },
        }
    )

    passed = all(check["status"] == "pass" for check in checks)
    report = {
        "version": "0.3.3",
        "batchId": BATCH_ID,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "result": "PASS_OFFLINE_UNITY_PENDING" if passed else "FAIL",
        "checks": checks,
    }
    REPORT_PATH.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(report["result"])
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
