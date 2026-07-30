from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[1]
PROJECT_ROOT = (
    REPO_ROOT
    / "ui-concepts"
    / "phase-9c"
    / "light-storybook-production-v0.1"
)
OUTPUT_PATH = PROJECT_ROOT / "PRODUCTION-MANIFEST-v0.3.3.json"
MINION_PATH = (
    REPO_ROOT
    / "sc"
    / "Assets"
    / "Resources"
    / "Configs"
    / "Json"
    / "minions.v0.1.json"
)
SPELL_PATH = (
    REPO_ROOT
    / "sc"
    / "Assets"
    / "Resources"
    / "Configs"
    / "Json"
    / "spells.v0.1.json"
)
BASELINE_CATALOG_PATH = (
    REPO_ROOT
    / "sc"
    / "Assets"
    / "Configs"
    / "Presentation"
    / "PresentationSpriteCatalog_LightStorybookFormalCatalogV032.asset"
)
STYLE_TILE_PATH = (
    REPO_ROOT
    / "ui-concepts"
    / "phase-9b"
    / "style-tiles"
    / "style-tile-d-wandering-storybook-v0.1.png"
)
PROMPT_PATH = (
    PROJECT_ROOT
    / "freeze-v0.3.3"
    / "PRODUCTION-PROMPTS-v0.3.3.zh-CN.md"
)
BATCH_ONE_ROOT = (
    PROJECT_ROOT / "validation-round-8-v0.3.3-tier1-production"
)
BATCH_TWO_ROOT = (
    PROJECT_ROOT / "validation-round-9-v0.3.3-tier2-production"
)
BATCH_THREE_ROOT = (
    PROJECT_ROOT / "validation-round-10-v0.3.3-tier3-production"
)
BATCH_FOUR_ROOT = (
    PROJECT_ROOT / "validation-round-11-v0.3.3-tier4-production"
)
BATCH_FIVE_ROOT = (
    PROJECT_ROOT / "validation-round-12-v0.3.3-tier5-production"
)
BATCH_SIX_ROOT = (
    PROJECT_ROOT / "validation-round-13-v0.3.3-spell-production"
)

EXPECTED_COUNTS = {
    "content": 83,
    "coveredContent": 32,
    "remaining": 51,
    "remainingMinions": 42,
    "remainingSpells": 9,
}
RACE_FOLDERS = {
    "ForgeSoul": "forge-soul",
    "WildSpirit": "wild-spirit",
    "Starbound": "starbound",
    "Wayfarer": "wayfarer",
}
BATCH_IDS = {
    1: "batch-01-tier1",
    2: "batch-02-tier2",
    3: "batch-03-tier3",
    4: "batch-04-tier4",
    5: "batch-05-tier5",
}


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def relative(path: Path) -> str:
    return path.relative_to(REPO_ROOT).as_posix()


def covered_art_ids() -> set[str]:
    text = BASELINE_CATALOG_PATH.read_text(encoding="utf-8")
    return set(re.findall(r"^  - id: (.+)$", text, flags=re.MULTILINE))


def batch_id(kind: str, tier: int) -> str:
    if kind == "Spell":
        return "batch-06-spells"
    return BATCH_IDS[tier]


def output_path(kind: str, config: dict[str, Any]) -> Path:
    slug = config["id"].replace("_", "-") + "-v0.3.3.png"
    if kind == "Spell":
        return BATCH_SIX_ROOT / "spells" / slug
    if config["tier"] == 1:
        return (
            BATCH_ONE_ROOT
            / "cards"
            / RACE_FOLDERS[config["race"]]
            / slug
        )
    if config["tier"] == 2:
        return (
            BATCH_TWO_ROOT
            / "cards"
            / RACE_FOLDERS[config["race"]]
            / slug
        )
    if config["tier"] == 3:
        return (
            BATCH_THREE_ROOT
            / "cards"
            / RACE_FOLDERS[config["race"]]
            / slug
        )
    if config["tier"] == 4:
        return (
            BATCH_FOUR_ROOT
            / "cards"
            / RACE_FOLDERS[config["race"]]
            / slug
        )
    if config["tier"] == 5:
        return (
            BATCH_FIVE_ROOT
            / "cards"
            / RACE_FOLDERS[config["race"]]
            / slug
        )
    return (
        PROJECT_ROOT
        / "production-v0.3.3"
        / "cards"
        / RACE_FOLDERS[config["race"]]
        / slug
    )


def entry(kind: str, config: dict[str, Any]) -> dict[str, Any]:
    art_path = output_path(kind, config)
    result: dict[str, Any] = {
        "kind": kind,
        "id": config["id"],
        "name": config["name"],
        "tier": config["tier"],
        "artId": config["artId"],
        "batchId": batch_id(kind, config["tier"]),
        "status": "generated" if art_path.is_file() else "pending",
        "artFile": relative(art_path),
        "focalPointY": 0.5,
        "description": config["description"],
        "tags": config.get("tags", []),
    }
    if kind == "Minion":
        result.update(
            {
                "race": config["race"],
                "attack": config["attack"],
                "health": config["health"],
                "goldenAttack": config["goldenAttack"],
                "goldenHealth": config["goldenHealth"],
                "keywords": config.get("keywords", []),
                "goldenDescription": config["goldenDescription"],
            }
        )
    else:
        result["cost"] = config["cost"]
    if art_path.is_file():
        result["sha256"] = sha256(art_path)
    return result


def build_manifest() -> dict[str, Any]:
    minions = load_json(MINION_PATH)["minions"]
    spells = load_json(SPELL_PATH)["spells"]
    covered = covered_art_ids()
    content = [
        ("Minion", value) for value in minions if value.get("enabled")
    ] + [("Spell", value) for value in spells if value.get("enabled")]
    remaining = [
        entry(kind, value)
        for kind, value in content
        if value["artId"] not in covered
    ]
    remaining.sort(
        key=lambda value: (
            value["batchId"],
            value.get("race", ""),
            value["tier"],
            value["id"],
        )
    )

    counts = {
        "content": len(content),
        "baselineCatalogEntries": len(covered),
        "coveredContent": sum(
            value["artId"] in covered for _, value in content
        ),
        "remaining": len(remaining),
        "remainingMinions": sum(
            value["kind"] == "Minion" for value in remaining
        ),
        "remainingSpells": sum(
            value["kind"] == "Spell" for value in remaining
        ),
        "generated": sum(
            value["status"] == "generated" for value in remaining
        ),
        "pending": sum(
            value["status"] == "pending" for value in remaining
        ),
    }
    for key, expected in EXPECTED_COUNTS.items():
        if counts[key] != expected:
            raise RuntimeError(
                f"Production scope drifted: {key}={counts[key]}, "
                f"expected {expected}."
            )
    batch_counts: dict[str, int] = {}
    for value in remaining:
        batch_counts[value["batchId"]] = (
            batch_counts.get(value["batchId"], 0) + 1
        )
    if batch_counts != {
        "batch-01-tier1": 7,
        "batch-02-tier2": 11,
        "batch-03-tier3": 7,
        "batch-04-tier4": 11,
        "batch-05-tier5": 6,
        "batch-06-spells": 9,
    }:
        raise RuntimeError(
            f"Production batch counts drifted: {batch_counts}"
        )

    return {
        "version": "0.3.3",
        "purpose": (
            "remaining formal card-art production after the isolated "
            "v0.3.2 catalog baseline"
        ),
        "runtimePolicy": (
            "Calibration only. Do not modify the Runtime catalog until "
            "the promoted candidate is explicitly approved."
        ),
        "sources": {
            "minions": {
                "path": relative(MINION_PATH),
                "sha256": sha256(MINION_PATH),
            },
            "spells": {
                "path": relative(SPELL_PATH),
                "sha256": sha256(SPELL_PATH),
            },
            "baselineCatalog": {
                "path": relative(BASELINE_CATALOG_PATH),
                "sha256": sha256(BASELINE_CATALOG_PATH),
            },
            "styleTile": {
                "path": relative(STYLE_TILE_PATH),
                "sha256": sha256(STYLE_TILE_PATH),
            },
            "productionPrompt": {
                "path": relative(PROMPT_PATH),
                "sha256": sha256(PROMPT_PATH),
            },
        },
        "counts": counts,
        "batchCounts": batch_counts,
        "items": remaining,
    }


def serialized_manifest() -> str:
    return (
        json.dumps(
            build_manifest(),
            ensure_ascii=False,
            indent=2,
        )
        + "\n"
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--check",
        action="store_true",
        help="Fail if the checked-in manifest differs from generated output.",
    )
    args = parser.parse_args()
    expected = serialized_manifest()
    if args.check:
        if not OUTPUT_PATH.is_file():
            print(f"missing: {relative(OUTPUT_PATH)}")
            return 1
        if OUTPUT_PATH.read_text(encoding="utf-8") != expected:
            print(f"stale: {relative(OUTPUT_PATH)}")
            return 1
        print("production manifest is current")
        return 0
    OUTPUT_PATH.write_text(expected, encoding="utf-8", newline="\n")
    print(f"wrote {relative(OUTPUT_PATH)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
