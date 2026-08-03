"""Offline consistency checks for the v0.4.0 journal artwork package.

This verifies only immutable files and import contracts. Unity import, Player
capture, and visual sign-off remain separate release gates.
"""

import hashlib
import json
import struct
import sys
from pathlib import Path


PACKAGE_ROOT = Path(__file__).resolve().parent
REPO_ROOT = PACKAGE_ROOT.parents[3]
MANIFEST_PATH = PACKAGE_ROOT / "ASSET-MANIFEST-v0.4.0.json"
REPORT_PATH = PACKAGE_ROOT / "JOURNAL-ASSET-VALIDATION-REPORT.json"
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def png_dimensions(path: Path) -> tuple[int, int]:
    header = path.read_bytes()[:24]
    if len(header) != 24 or header[:8] != PNG_SIGNATURE:
        raise ValueError("not a PNG file")
    if header[12:16] != b"IHDR":
        raise ValueError("missing IHDR chunk")
    return struct.unpack(">II", header[16:24])


def check_asset(asset: dict, errors: list[str]) -> dict:
    path = REPO_ROOT / asset["assetPath"]
    meta_path = Path(f"{path}.meta")
    result = {"id": asset["id"], "path": asset["assetPath"]}
    if not path.is_file():
        errors.append(f"missing artwork: {asset['assetPath']}")
        return result

    try:
        width, height = png_dimensions(path)
        result["width"] = width
        result["height"] = height
        if (width, height) != (asset["width"], asset["height"]):
            errors.append(
                f"dimension mismatch for {asset['id']}: "
                f"{width}x{height} != {asset['width']}x{asset['height']}"
            )
    except ValueError as error:
        errors.append(f"invalid PNG {asset['assetPath']}: {error}")

    result["sha256"] = sha256(path)
    if result["sha256"] != asset["sha256"]:
        errors.append(f"hash mismatch for {asset['id']}")

    if not meta_path.is_file():
        errors.append(f"missing import meta: {meta_path.relative_to(REPO_ROOT)}")
        return result

    meta = meta_path.read_text(encoding="utf-8")
    for required in ("textureType: 8", "spriteMode: 1", "enableMipMap: 0"):
        if required not in meta:
            errors.append(
                f"missing Sprite import contract '{required}' for {asset['id']}"
            )
    return result


def main() -> int:
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    errors: list[str] = []
    style = manifest["styleReference"]
    style_path = REPO_ROOT / style["path"]
    if not style_path.is_file():
        errors.append(f"missing Style Tile: {style['path']}")
    elif sha256(style_path) != style["sha256"]:
        errors.append("Style Tile hash changed; regenerate or explicitly re-baseline.")

    assets = [check_asset(asset, errors) for asset in manifest["assets"]]
    report = {
        "schemaVersion": manifest["schemaVersion"],
        "status": "FAIL" if errors else "PASS_OFFLINE_UNITY_PENDING",
        "assetCount": len(assets),
        "assets": assets,
        "errors": errors,
        "note": (
            "Offline consistency only. Unity import, EditMode/PlayMode, Player "
            "screenshots, and human visual approval are still required."
        ),
    }
    REPORT_PATH.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
