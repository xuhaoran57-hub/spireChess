"""Prepare the approved phase-9b card art for Unity runtime use."""

from __future__ import annotations

import shutil
from pathlib import Path

from PIL import Image

from compose_card_component_validation_v02 import (
    cost_component,
    nine_slice_horizontal,
    tier_component,
)


ROOT = Path(__file__).resolve().parents[1]
MASTER_ART = (
    ROOT
    / "ui-concepts"
    / "phase-9b"
    / "archetype-anchor-illustrations-v0.2"
    / "masters"
)
MASTER_COMPONENTS = (
    ROOT
    / "ui-concepts"
    / "phase-9b"
    / "card-components-number-tags-v0.2"
    / "components"
)
UNITY_ART = ROOT / "sc" / "Assets" / "Art" / "Presentation"

ARTWORKS = (
    (
        "forge-soul-shield-squire.png",
        "Cards/Minions/ForgeSoul/card_minion_forge_soul_shield_squire.png",
    ),
    (
        "forge-undying-furnace-king.png",
        "Cards/Minions/ForgeSoul/card_minion_undying_furnace_king.png",
    ),
    (
        "wild-young-deer-spirit.png",
        "Cards/Minions/WildSpirit/card_minion_young_deer_spirit.png",
    ),
    (
        "wild-ten-thousand-hoof-surge.png",
        "Cards/Minions/WildSpirit/card_minion_ten_thousand_hoof_surge.png",
    ),
    (
        "star-astrolabe-calibrator.png",
        "Cards/Minions/Starbound/card_minion_astrolabe_calibrator.png",
    ),
    (
        "star-sky-covenant-bearer.png",
        "Cards/Minions/Starbound/card_minion_sky_covenant_bearer.png",
    ),
    (
        "wayfarer-traveling-physician.png",
        "Cards/Minions/Wayfarer/card_minion_traveling_physician.png",
    ),
    (
        "wayfarer-many-arts-apprentice.png",
        "Cards/Minions/Wayfarer/card_minion_many_arts_apprentice.png",
    ),
)

COMPONENT_OUTPUT = UNITY_ART / "UI" / "Card"

FOLDER_GUIDS = {
    "Cards/Minions/WildSpirit": "e98635ee2d9f4ae784ee9d5dbd7b1ba9",
    "Cards/Minions/Starbound": "a1944629a72d42efa1dd7e2cb7c7e19a",
    "Cards/Minions/Wayfarer": "8784088996304f53a4371c7beaa2bf3c",
    "UI/Card": "3c7d154d4a4d45079bbf4ba8ca0343f0",
}

TEXTURE_GUIDS = {
    "Cards/Minions/WildSpirit/card_minion_young_deer_spirit.png":
        "2d1f0e574b5b4882bbfb0231da3db462",
    "Cards/Minions/WildSpirit/card_minion_ten_thousand_hoof_surge.png":
        "050bf0f7d6ac4a05a10fc3f352c243f4",
    "Cards/Minions/Starbound/card_minion_astrolabe_calibrator.png":
        "14433058cad447eeac1f6e3495056bba",
    "Cards/Minions/Starbound/card_minion_sky_covenant_bearer.png":
        "445cc5b4c7764c86be86da345e405d03",
    "Cards/Minions/Wayfarer/card_minion_traveling_physician.png":
        "078cfc8112384ed4ac029ae18f7fef44",
    "Cards/Minions/Wayfarer/card_minion_many_arts_apprentice.png":
        "b9cd43cae9af4522b6897e6fdb4b03a9",
    "UI/Card/card_cost_coin_v1.png":
        "438cc794c78b475ca1efc0bb49b9b09f",
    "UI/Card/card_tier_bookmark_v1.png":
        "dbd00599fd3c4fe6a7deb74f04fc0d1c",
    "UI/Card/card_attack_tag_v1.png":
        "cfe9e1fc3c3441e2b0d6e31d853999d4",
    "UI/Card/card_health_tag_v1.png":
        "3afe4833f6a44828baeb6a6cc4efbb71",
}


def write_if_missing(path: Path, content: str) -> None:
    if path.exists():
        return
    path.write_text(content, encoding="utf-8", newline="\n")


def folder_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def texture_meta(
    guid: str,
    *,
    pixels_per_unit: int,
    max_texture_size: int,
    alpha_transparency: bool,
    border: tuple[int, int, int, int] = (0, 0, 0, 0),
) -> str:
    alpha = 1 if alpha_transparency else 0
    left, bottom, right, top = border
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {max_texture_size}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {pixels_per_unit}
  spriteBorder: {{x: {left}, y: {bottom}, z: {right}, w: {top}}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: {alpha}
  alphaIsTransparency: {alpha}
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {max_texture_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def prepare_unity_meta() -> None:
    for relative_path, guid in FOLDER_GUIDS.items():
        write_if_missing(
            (UNITY_ART / relative_path).with_suffix(".meta"),
            folder_meta(guid),
        )

    component_settings = {
        "UI/Card/card_cost_coin_v1.png": (0, 0, 0, 0),
        "UI/Card/card_tier_bookmark_v1.png": (0, 0, 0, 0),
        "UI/Card/card_attack_tag_v1.png": (58, 16, 25, 16),
        "UI/Card/card_health_tag_v1.png": (25, 16, 69, 16),
    }
    for relative_path, guid in TEXTURE_GUIDS.items():
        is_component = relative_path in component_settings
        write_if_missing(
            Path(str(UNITY_ART / relative_path) + ".meta"),
            texture_meta(
                guid,
                pixels_per_unit=400 if is_component else 100,
                max_texture_size=512 if is_component else 2048,
                alpha_transparency=is_component,
                border=component_settings.get(relative_path, (0, 0, 0, 0)),
            ),
        )


def prepare_artworks() -> None:
    for source_name, relative_target in ARTWORKS:
        source = MASTER_ART / source_name
        target = UNITY_ART / relative_target
        with Image.open(source) as image:
            if image.size != (1024, 1536):
                raise ValueError(
                    f"Unexpected approved artwork size {image.size}: {source}"
                )
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)


def prepare_components() -> None:
    COMPONENT_OUTPUT.mkdir(parents=True, exist_ok=True)
    with Image.open(MASTER_COMPONENTS / "cost-coin.png") as source:
        cost_component(source, (112, 116)).save(
            COMPONENT_OUTPUT / "card_cost_coin_v1.png"
        )
    with Image.open(MASTER_COMPONENTS / "tier-bookmark.png") as source:
        tier_component(source, (84, 112)).save(
            COMPONENT_OUTPUT / "card_tier_bookmark_v1.png"
        )
    with Image.open(MASTER_COMPONENTS / "attack-tag.png") as source:
        nine_slice_horizontal(
            source,
            (220, 88),
            source_left_fraction=0.14,
            source_right_fraction=0.055,
            target_left_factor=0.66,
            target_right_factor=0.28,
        ).save(COMPONENT_OUTPUT / "card_attack_tag_v1.png")
    with Image.open(MASTER_COMPONENTS / "health-tag.png") as source:
        nine_slice_horizontal(
            source,
            (220, 88),
            source_left_fraction=0.055,
            source_right_fraction=0.20,
            target_left_factor=0.28,
            target_right_factor=0.78,
        ).save(COMPONENT_OUTPUT / "card_health_tag_v1.png")


def main() -> None:
    prepare_artworks()
    prepare_components()
    prepare_unity_meta()
    print("Prepared 8 artworks and 4 numeric components for Unity.")


if __name__ == "__main__":
    main()
