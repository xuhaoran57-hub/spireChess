"""Compose the post-illustration-blind-test card set for Phase 9B.

The script uses the approved archetype anchor illustrations, runtime minion
copy, the project's native storybook frame, and the lightweight v0.2 numeric
components. Full and Compact outputs are rendered separately so Compact can
use a short summary while both sizes draw numeric text at final resolution.
"""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageOps

from compose_card_component_validation_v02 import (
    FRAME_PATH,
    FRAME_SIZE,
    LAYOUTS,
    add_numeric_components,
    add_paper_grain,
    draw_centered_text,
    font,
    load_components,
    rounded_mask,
    wrap_characters,
)


ROOT = Path(__file__).resolve().parent.parent
OUT = (
    ROOT
    / "ui-concepts"
    / "phase-9b"
    / "archetype-card-blind-test-v0.2"
)
ART_ROOT = (
    ROOT
    / "ui-concepts"
    / "phase-9b"
    / "archetype-anchor-illustrations-v0.2"
    / "masters"
)
CONFIG_PATH = (
    ROOT
    / "sc"
    / "Assets"
    / "Resources"
    / "Configs"
    / "Json"
    / "minions.v0.1.json"
)

FULL_DIR = OUT / "full-240x360"
COMPACT_DIR = OUT / "compact-160x240"
STRESS_DIR = OUT / "four-digit-stress"
REVIEW_DIR = OUT / "review"

RACE_STYLES = {
    "ForgeSoul": {
        "label": "铸魂  FORGE SOUL",
        "short": "铸魂",
        "name_fill": (53, 40, 35, 252),
        "accent": (157, 72, 39, 232),
        "text": (239, 193, 147, 255),
    },
    "WildSpirit": {
        "label": "荒灵  WILD SPIRIT",
        "short": "荒灵",
        "name_fill": (44, 58, 36, 252),
        "accent": (99, 126, 61, 232),
        "text": (201, 220, 159, 255),
    },
    "Starbound": {
        "label": "星契  STAR PACT",
        "short": "星契",
        "name_fill": (34, 43, 73, 252),
        "accent": (66, 86, 148, 232),
        "text": (190, 205, 242, 255),
    },
    "Wayfarer": {
        "label": "旅团  BRIGADE",
        "short": "旅团",
        "name_fill": (67, 53, 42, 252),
        "accent": (139, 103, 66, 232),
        "text": (230, 204, 166, 255),
    },
}

KEYWORD_NAMES = {
    "Taunt": "嘲讽",
    "Battlecry": "战吼",
    "Deathrattle": "亡语",
    "Shield": "护盾",
    "Splash": "溅射",
}

CARD_SPECS = (
    {
        "id": "forge_soul_shield_squire",
        "file": "forge-soul-shield-squire.png",
        "motif": "护盾前排",
        "compact": "嘲讽 · 开场护盾",
        "art_center_y": 0.31,
    },
    {
        "id": "undying_furnace_king",
        "file": "forge-undying-furnace-king.png",
        "motif": "护盾核心",
        "compact": "嘲讽 · 开场护盾 · 护盾转移×2",
        "art_center_y": 0.18,
    },
    {
        "id": "young_deer_spirit",
        "file": "wild-young-deer-spirit.png",
        "motif": "亡语召唤",
        "compact": "亡语召唤 · 满场补偿",
        "art_center_y": 0.27,
    },
    {
        "id": "ten_thousand_hoof_surge",
        "file": "wild-ten-thousand-hoof-surge.png",
        "motif": "奔潮核心",
        "compact": "召唤强化 · 立即攻击 · 永久成长",
        "art_center_y": 0.27,
    },
    {
        "id": "astrolabe_calibrator",
        "file": "star-astrolabe-calibrator.png",
        "motif": "刷新校准",
        "compact": "首次刷新 · 最低攻击永久+1",
        "art_center_y": 0.27,
    },
    {
        "id": "sky_covenant_bearer",
        "file": "star-sky-covenant-bearer.png",
        "motif": "刷新成长",
        "compact": "每4次刷新 · 星契全体永久+1/+1",
        "art_center_y": 0.25,
    },
    {
        "id": "traveling_physician",
        "file": "wayfarer-traveling-physician.png",
        "motif": "旅团保血",
        "compact": "战吼 · 另一友方永久+1生命",
        "art_center_y": 0.27,
    },
    {
        "id": "many_arts_apprentice",
        "file": "wayfarer-many-arts-apprentice.png",
        "motif": "关键词借用",
        "compact": "开场复制左侧三类关键词",
        "art_center_y": 0.27,
    },
)


def load_card_config() -> dict[str, dict]:
    document = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    cards = {card["id"]: card for card in document["minions"]}
    missing = [spec["id"] for spec in CARD_SPECS if spec["id"] not in cards]
    if missing:
        raise ValueError(f"Missing card config entries: {missing}")
    return cards


def type_line(card: dict) -> str:
    style = RACE_STYLES[card["race"]]
    keywords = [
        KEYWORD_NAMES.get(keyword, keyword)
        for keyword in card.get("keywords", [])
    ]
    if not keywords:
        return style["short"]
    return f"{style['short']} · {' · '.join(keywords)}"


def compose_native_base(
    card: dict,
    spec: dict,
    frame: Image.Image,
    compact: bool,
) -> Image.Image:
    """Build one native-size card base before final-size numeric overlays."""

    art_path = ART_ROOT / spec["file"]
    art = Image.open(art_path).convert("RGB")
    if art.size != (1024, 1536):
        raise ValueError(f"Unexpected art size for {art_path}: {art.size}")

    canvas = Image.new("RGBA", FRAME_SIZE, (0, 0, 0, 0))
    art_box = (94, 99, 923, 918)
    art_crop = ImageOps.fit(
        art,
        (art_box[2] - art_box[0], art_box[3] - art_box[1]),
        Image.Resampling.LANCZOS,
        centering=(0.5, spec["art_center_y"]),
    ).convert("RGBA")
    art_mask = rounded_mask(FRAME_SIZE, art_box, 100)
    canvas.paste(art_crop, (art_box[0], art_box[1]), art_mask.crop(art_box))

    style = RACE_STYLES[card["race"]]
    draw = ImageDraw.Draw(canvas, "RGBA")
    name_box = (101, 925, 916, 1049)
    info_box = (96, 1056, 921, 1484)
    draw.rounded_rectangle(
        name_box,
        radius=55,
        fill=style["name_fill"],
        outline=style["accent"],
        width=3,
    )
    draw.rounded_rectangle(
        info_box,
        radius=96,
        fill=(236, 221, 184, 255),
        outline=style["accent"],
        width=3,
    )
    add_paper_grain(
        canvas,
        info_box,
        seed=1900 + card["tier"] * 97,
        count=6800,
    )

    # Keep the approved project frame at its native pixels.
    canvas = Image.alpha_composite(canvas, frame)
    content = Image.new("RGBA", FRAME_SIZE, (0, 0, 0, 0))
    draw = ImageDraw.Draw(content, "RGBA")

    draw_centered_text(
        draw,
        (150, 938, 867, 1012),
        card["name"],
        font(66),
        (247, 236, 207, 255),
        (32, 25, 22, 220),
        2,
    )
    draw_centered_text(
        draw,
        (150, 1005, 867, 1037),
        type_line(card),
        font(28),
        style["text"],
    )

    draw.line((184, 1119, 833, 1119), fill=style["accent"], width=2)
    draw_centered_text(
        draw,
        (190, 1078, 827, 1120),
        spec["motif"],
        font(29),
        (105, 72, 44, 255),
    )

    description = spec["compact"] if compact else card["description"]
    description_font = font(42 if compact else 34)
    description_box = (158, 1142, 859, 1402)
    lines = wrap_characters(
        draw,
        description,
        description_font,
        description_box[2] - description_box[0],
        2 if compact else 4,
    )
    line_height = 58 if compact else 49
    total_height = len(lines) * line_height
    y = description_box[1] + max(
        0,
        (description_box[3] - description_box[1] - total_height) // 2,
    )
    for line in lines:
        draw_centered_text(
            draw,
            (description_box[0], y, description_box[2], y + line_height),
            line,
            description_font,
            (72, 52, 36, 255),
        )
        y += line_height

    content.putalpha(
        ImageChops.multiply(
            content.getchannel("A"),
            ImageOps.invert(frame.getchannel("A")),
        )
    )
    return Image.alpha_composite(canvas, content)


def render_card(
    card: dict,
    spec: dict,
    frame: Image.Image,
    components: dict[str, Image.Image],
    layout_name: str,
    attack: str | None = None,
    health: str | None = None,
) -> Image.Image:
    compact = layout_name == "compact"
    base = compose_native_base(card, spec, frame, compact)
    layout = LAYOUTS[layout_name]
    target = base.resize(layout["size"], Image.Resampling.LANCZOS)
    values = (
        "3",
        str(card["tier"]),
        attack or str(card["attack"]),
        health or str(card["health"]),
    )
    return add_numeric_components(target, layout, components, values)


def paper_canvas(
    size: tuple[int, int],
    seed: int,
    grain_count: int,
) -> Image.Image:
    canvas = Image.new("RGBA", size, (236, 219, 181, 255))
    add_paper_grain(canvas, (0, 0, size[0], size[1]), seed, grain_count)
    return canvas


def draw_race_header(
    sheet: Image.Image,
    box: tuple[int, int, int, int],
    style: dict,
) -> None:
    draw = ImageDraw.Draw(sheet, "RGBA")
    draw.rounded_rectangle(
        box,
        radius=14,
        fill=style["accent"],
        outline=(85, 58, 36, 110),
        width=1,
    )
    draw_centered_text(
        draw,
        box,
        style["label"],
        font(19),
        (250, 239, 211, 255),
    )


def build_full_review(cards: dict[str, Image.Image]) -> Image.Image:
    sheet = paper_canvas((1200, 940), seed=4201, grain_count=12000)
    draw = ImageDraw.Draw(sheet, "RGBA")
    draw_centered_text(
        draw,
        (60, 20, 1140, 66),
        "三流派＋旅团 · 实际卡面集成",
        font(35),
        (69, 48, 32, 255),
    )
    draw_centered_text(
        draw,
        (60, 64, 1140, 91),
        "FULL 240×360｜最新立绘｜真实配置｜项目卡框＋轻量数值组件",
        font(17),
        (125, 87, 54, 235),
    )

    for column in range(4):
        first_spec = CARD_SPECS[column * 2]
        card_id = first_spec["id"]
        x = 65 + column * 285
        race = RACE_STYLES[
            {
                0: "ForgeSoul",
                1: "WildSpirit",
                2: "Starbound",
                3: "Wayfarer",
            }[column]
        ]
        draw_race_header(sheet, (x - 4, 98, x + 244, 128), race)
        sheet.alpha_composite(cards[card_id], (x, 140))
        sheet.alpha_composite(
            cards[CARD_SPECS[column * 2 + 1]["id"]],
            (x, 520),
        )

    draw_centered_text(
        ImageDraw.Draw(sheet, "RGBA"),
        (80, 894, 1120, 925),
        "正式八卡均使用真实 3 费、等级与攻血；四位数只在独立压力样板出现",
        font(17),
        (125, 87, 54, 235),
    )
    return sheet


def build_compact_review(cards: dict[str, Image.Image]) -> Image.Image:
    sheet = paper_canvas((940, 690), seed=4202, grain_count=9000)
    draw = ImageDraw.Draw(sheet, "RGBA")
    draw_centered_text(
        draw,
        (50, 18, 890, 61),
        "三流派＋旅团 · Compact 可读性",
        font(31),
        (69, 48, 32, 255),
    )
    draw_centered_text(
        draw,
        (50, 59, 890, 85),
        "COMPACT 160×240｜两行机制摘要｜数字在最终分辨率直接绘制",
        font(16),
        (125, 87, 54, 235),
    )

    race_ids = ("ForgeSoul", "WildSpirit", "Starbound", "Wayfarer")
    for column, race_id in enumerate(race_ids):
        x = 60 + column * 220
        draw_race_header(
            sheet,
            (x - 4, 92, x + 164, 120),
            RACE_STYLES[race_id],
        )
        sheet.alpha_composite(
            cards[CARD_SPECS[column * 2]["id"]],
            (x, 130),
        )
        sheet.alpha_composite(
            cards[CARD_SPECS[column * 2 + 1]["id"]],
            (x, 390),
        )

    draw_centered_text(
        ImageDraw.Draw(sheet, "RGBA"),
        (60, 644, 880, 675),
        "本样板保留费用作组件压力验证；Runtime owned Compact 仍按契约隐藏费用",
        font(16),
        (125, 87, 54, 235),
    )
    return sheet


def build_stress_review(
    full_card: Image.Image,
    compact_card: Image.Image,
) -> Image.Image:
    sheet = paper_canvas((820, 520), seed=4203, grain_count=7200)
    draw = ImageDraw.Draw(sheet, "RGBA")
    draw_centered_text(
        draw,
        (50, 22, 770, 68),
        "不熄炉王 · 四位数压力样板",
        font(32),
        (69, 48, 32, 255),
    )
    draw_centered_text(
        draw,
        (50, 66, 770, 93),
        "9999 攻击 / 1200 生命仅用于边界验证，不是正式卡牌数值",
        font(16),
        (125, 87, 54, 235),
    )

    sheet.alpha_composite(full_card, (100, 116))
    sheet.alpha_composite(compact_card, (505, 176))
    draw_centered_text(
        draw,
        (76, 480, 364, 508),
        "FULL 240×360",
        font(18),
        (69, 48, 32, 255),
    )
    draw_centered_text(
        draw,
        (475, 430, 695, 458),
        "COMPACT 160×240",
        font(18),
        (69, 48, 32, 255),
    )
    return sheet


def validate_outputs(
    full_cards: dict[str, Image.Image],
    compact_cards: dict[str, Image.Image],
    stress_full: Image.Image,
    stress_compact: Image.Image,
) -> None:
    expected_ids = {spec["id"] for spec in CARD_SPECS}
    if set(full_cards) != expected_ids or set(compact_cards) != expected_ids:
        raise ValueError("Card output set does not match the eight requested IDs")
    for label, cards, expected_size in (
        ("full", full_cards, (240, 360)),
        ("compact", compact_cards, (160, 240)),
    ):
        for card_id, image in cards.items():
            if image.size != expected_size:
                raise ValueError(
                    f"{label} {card_id} has size {image.size}, "
                    f"expected {expected_size}"
                )
            if image.getbbox() is None:
                raise ValueError(f"{label} {card_id} is fully transparent")
    if stress_full.size != (240, 360):
        raise ValueError(f"Stress Full has wrong size: {stress_full.size}")
    if stress_compact.size != (160, 240):
        raise ValueError(f"Stress Compact has wrong size: {stress_compact.size}")


def main() -> None:
    for directory in (FULL_DIR, COMPACT_DIR, STRESS_DIR, REVIEW_DIR):
        directory.mkdir(parents=True, exist_ok=True)

    frame = Image.open(FRAME_PATH).convert("RGBA")
    if frame.size != FRAME_SIZE:
        raise ValueError(f"Unexpected frame size: {frame.size}")
    components = load_components()
    card_config = load_card_config()

    full_cards: dict[str, Image.Image] = {}
    compact_cards: dict[str, Image.Image] = {}
    for spec in CARD_SPECS:
        card = card_config[spec["id"]]
        full = render_card(card, spec, frame, components, "full")
        compact = render_card(card, spec, frame, components, "compact")
        full_cards[spec["id"]] = full
        compact_cards[spec["id"]] = compact
        slug = Path(spec["file"]).stem
        full.save(FULL_DIR / f"{slug}.png", optimize=True)
        compact.save(COMPACT_DIR / f"{slug}.png", optimize=True)

    furnace_spec = next(
        spec for spec in CARD_SPECS if spec["id"] == "undying_furnace_king"
    )
    furnace_card = card_config["undying_furnace_king"]
    stress_full = render_card(
        furnace_card,
        furnace_spec,
        frame,
        components,
        "full",
        attack="9999",
        health="1200",
    )
    stress_compact = render_card(
        furnace_card,
        furnace_spec,
        frame,
        components,
        "compact",
        attack="9999",
        health="1200",
    )
    validate_outputs(full_cards, compact_cards, stress_full, stress_compact)

    stress_full.save(
        STRESS_DIR / "undying-furnace-king-9999-1200-full-240x360.png",
        optimize=True,
    )
    stress_compact.save(
        STRESS_DIR / "undying-furnace-king-9999-1200-compact-160x240.png",
        optimize=True,
    )
    build_full_review(full_cards).save(
        REVIEW_DIR / "eight-cards-full-240x360-v0.2.png",
        optimize=True,
    )
    build_compact_review(compact_cards).save(
        REVIEW_DIR / "eight-cards-compact-160x240-v0.2.png",
        optimize=True,
    )
    build_stress_review(stress_full, stress_compact).save(
        REVIEW_DIR / "four-digit-stress-v0.2.png",
        optimize=True,
    )


if __name__ == "__main__":
    main()
