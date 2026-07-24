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
SAMPLE_MASTER_ART = (
    ROOT
    / "ui-concepts"
    / "phase-9b"
    / "sample-minion-illustrations-v0.1"
    / "masters"
)
G2_MASTER_ART = (
    ROOT
    / "ui-concepts"
    / "phase-9b"
    / "g2-card-assets-v0.1"
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
        MASTER_ART,
        "forge-soul-shield-squire.png",
        "Cards/Minions/ForgeSoul/card_minion_forge_soul_shield_squire.png",
    ),
    (
        MASTER_ART,
        "forge-undying-furnace-king.png",
        "Cards/Minions/ForgeSoul/card_minion_undying_furnace_king.png",
    ),
    (
        MASTER_ART,
        "wild-young-deer-spirit.png",
        "Cards/Minions/WildSpirit/card_minion_young_deer_spirit.png",
    ),
    (
        MASTER_ART,
        "wild-ten-thousand-hoof-surge.png",
        "Cards/Minions/WildSpirit/card_minion_ten_thousand_hoof_surge.png",
    ),
    (
        MASTER_ART,
        "star-astrolabe-calibrator.png",
        "Cards/Minions/Starbound/card_minion_astrolabe_calibrator.png",
    ),
    (
        MASTER_ART,
        "star-sky-covenant-bearer.png",
        "Cards/Minions/Starbound/card_minion_sky_covenant_bearer.png",
    ),
    (
        MASTER_ART,
        "wayfarer-traveling-physician.png",
        "Cards/Minions/Wayfarer/card_minion_traveling_physician.png",
    ),
    (
        MASTER_ART,
        "wayfarer-many-arts-apprentice.png",
        "Cards/Minions/Wayfarer/card_minion_many_arts_apprentice.png",
    ),
    (
        SAMPLE_MASTER_ART,
        "forge-tempering-mender.png",
        "Cards/Minions/ForgeSoul/card_minion_tempering_mender.png",
    ),
    (
        SAMPLE_MASTER_ART,
        "forge-cracked-armor-avenger.png",
        "Cards/Minions/ForgeSoul/card_minion_cracked_armor_avenger.png",
    ),
    (
        SAMPLE_MASTER_ART,
        "wild-rotleaf-heir.png",
        "Cards/Minions/WildSpirit/card_minion_rotleaf_heir.png",
    ),
    (
        SAMPLE_MASTER_ART,
        "wild-fox-den-matriarch.png",
        "Cards/Minions/WildSpirit/card_minion_fox_den_matriarch.png",
    ),
    (
        SAMPLE_MASTER_ART,
        "star-secret-page-refractor.png",
        "Cards/Minions/Starbound/card_minion_secret_page_refractor.png",
    ),
    (
        SAMPLE_MASTER_ART,
        "star-star-map-broker.png",
        "Cards/Minions/Starbound/card_minion_star_map_broker.png",
    ),
)

G2_ARTWORKS = (
    (
        "token-young-spirit.png",
        "Cards/Tokens/card_token_token_young_spirit.png",
        (1024, 1536),
    ),
    (
        "token-two-tailed-fox-shadow.png",
        "Cards/Tokens/card_token_token_two_tailed_fox_shadow.png",
        (1024, 1536),
    ),
    (
        "token-swift-young-spirit.png",
        "Cards/Tokens/card_token_token_swift_young_spirit.png",
        (1024, 1536),
    ),
    (
        "spell-minor-tempering.png",
        "Cards/Spells/card_spell_minor_tempering.png",
        (1024, 1536),
    ),
    (
        "spell-free-refresh.png",
        "Cards/Spells/card_spell_free_refresh.png",
        (1024, 1536),
    ),
    (
        "spell-advanced-discovery.png",
        "Cards/Spells/card_spell_advanced_discovery.png",
        (1024, 1536),
    ),
    (
        "spell-prebattle-benediction.png",
        "Cards/Spells/card_spell_prebattle_benediction.png",
        (1024, 1536),
    ),
    (
        "relic-crown-echo-bell.png",
        "Icons/Relics/icon_relic_crown_echo_bell.png",
        (1254, 1254),
    ),
    (
        "relic-crown-thousand-shields.png",
        "Icons/Relics/icon_relic_crown_thousand_shields.png",
        (1254, 1254),
    ),
    (
        "relic-curio-refresh-gear.png",
        "Icons/Relics/icon_relic_curio_refresh_gear.png",
        (1254, 1254),
    ),
    (
        "diagnostic-missing-art.png",
        "UI/Diagnostics/fallback_missing_art.png",
        (1254, 1254),
    ),
)

COMPONENT_OUTPUT = UNITY_ART / "UI" / "Card"

FOLDER_GUIDS = {
    "Cards/Minions/WildSpirit": "e98635ee2d9f4ae784ee9d5dbd7b1ba9",
    "Cards/Minions/Starbound": "a1944629a72d42efa1dd7e2cb7c7e19a",
    "Cards/Minions/Wayfarer": "8784088996304f53a4371c7beaa2bf3c",
    "Cards/Spells": "9ba1f26a529166b4e828f63972f6e81d",
    "Cards/Tokens": "f6e795f6c33c4cd44864ad8154668748",
    "Icons": "054eb347674c52a40a1b1c8a2c5929f4",
    "Icons/Relics": "3f58d49d6d86306498cb8efe4b9ab385",
    "UI/Card": "3c7d154d4a4d45079bbf4ba8ca0343f0",
    "UI/Diagnostics": "80ae95c6a24a2e04db0f4d5a3f24c985",
}

TEXTURE_GUIDS = {
    "Cards/Minions/ForgeSoul/card_minion_tempering_mender.png":
        "68718a5dc430ca0488d2ac8d546ef074",
    "Cards/Minions/ForgeSoul/card_minion_cracked_armor_avenger.png":
        "1fa97e73403224d44acd6ee534e18663",
    "Cards/Minions/WildSpirit/card_minion_young_deer_spirit.png":
        "2d1f0e574b5b4882bbfb0231da3db462",
    "Cards/Minions/WildSpirit/card_minion_ten_thousand_hoof_surge.png":
        "050bf0f7d6ac4a05a10fc3f352c243f4",
    "Cards/Minions/WildSpirit/card_minion_rotleaf_heir.png":
        "3bd7fa03a5ef35e4d874b66616b73785",
    "Cards/Minions/WildSpirit/card_minion_fox_den_matriarch.png":
        "838a14903c8e7c04ab87597576da7ae8",
    "Cards/Minions/Starbound/card_minion_astrolabe_calibrator.png":
        "14433058cad447eeac1f6e3495056bba",
    "Cards/Minions/Starbound/card_minion_sky_covenant_bearer.png":
        "445cc5b4c7764c86be86da345e405d03",
    "Cards/Minions/Starbound/card_minion_secret_page_refractor.png":
        "85869952f0ebdf643aff589e2a576f85",
    "Cards/Minions/Starbound/card_minion_star_map_broker.png":
        "d285ff7a2f2f20a4d82c75e7534072cd",
    "Cards/Minions/Wayfarer/card_minion_traveling_physician.png":
        "078cfc8112384ed4ac029ae18f7fef44",
    "Cards/Minions/Wayfarer/card_minion_many_arts_apprentice.png":
        "b9cd43cae9af4522b6897e6fdb4b03a9",
    "Cards/Tokens/card_token_token_young_spirit.png":
        "cb13c5e0516cc6b4fa0ad202ede3ff59",
    "Cards/Tokens/card_token_token_two_tailed_fox_shadow.png":
        "a94c860707ffa1d42a57c039ec358fcd",
    "Cards/Tokens/card_token_token_swift_young_spirit.png":
        "d189b173395fb3d4db872486c8710606",
    "Cards/Spells/card_spell_minor_tempering.png":
        "e216b312639b4dc4091e841dbb40b465",
    "Cards/Spells/card_spell_free_refresh.png":
        "11ef1edeab6d09b41adf8708eed94265",
    "Cards/Spells/card_spell_advanced_discovery.png":
        "5c1b8d8124af603439943f0264a60c57",
    "Cards/Spells/card_spell_prebattle_benediction.png":
        "c0a551913238ea7469844b082302ad85",
    "Icons/Relics/icon_relic_crown_echo_bell.png":
        "fedd2bfeaaf0ea04cb8afb06acc86d92",
    "Icons/Relics/icon_relic_crown_thousand_shields.png":
        "6592b0b3dd7f58847b4fe6b359dc4e27",
    "Icons/Relics/icon_relic_curio_refresh_gear.png":
        "6675c1d951ac49449a8de11d015a1279",
    "UI/Diagnostics/fallback_missing_art.png":
        "c23ada02db666b0408db93b3f641b887",
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
                max_texture_size=512
                if (
                    is_component
                    or relative_path.startswith("Icons/Relics/")
                    or relative_path.startswith("UI/Diagnostics/")
                )
                else 2048,
                alpha_transparency=is_component,
                border=component_settings.get(relative_path, (0, 0, 0, 0)),
            ),
        )


def prepare_artworks() -> None:
    for source_root, source_name, relative_target in ARTWORKS:
        source = source_root / source_name
        target = UNITY_ART / relative_target
        with Image.open(source) as image:
            if image.size != (1024, 1536):
                raise ValueError(
                    f"Unexpected approved artwork size {image.size}: {source}"
                )
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)

    for source_name, relative_target, expected_size in G2_ARTWORKS:
        source = G2_MASTER_ART / source_name
        target = UNITY_ART / relative_target
        with Image.open(source) as image:
            if image.size != expected_size:
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
    print("Prepared 25 artworks and 4 numeric components for Unity.")


if __name__ == "__main__":
    main()
