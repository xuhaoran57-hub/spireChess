"""Compose the lightweight numeric-tag validation sheet for Phase 9B.

The card base is assembled at the exact 1017x1546 size of the project's
storybook frame. Full and Compact cards are then exported independently, and
their numeric components/text are drawn at the final target resolution so a
one-pixel outline remains a one-pixel outline in both layouts.
"""

from __future__ import annotations

import json
import random
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parent.parent
OUT = (
    ROOT
    / "ui-concepts"
    / "phase-9b"
    / "card-components-number-tags-v0.2"
)
COMPONENTS = OUT / "components"
FRAME_PATH = (
    ROOT
    / "sc"
    / "Assets"
    / "Art"
    / "Presentation"
    / "UI"
    / "Common"
    / "card_frame_storybook_normal_v2.png"
)
ART_PATH = (
    ROOT
    / "ui-concepts"
    / "phase-9b"
    / "archetype-blind-test-v0.1"
    / "masters"
    / "forge-undying-furnace-king.png"
)
FONT_PATH = (
    ROOT
    / "sc"
    / "Assets"
    / "Art"
    / "Fonts"
    / "NotoSansCJKsc-Regular.otf"
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

FRAME_SIZE = (1017, 1546)
FULL_SIZE = (240, 360)
COMPACT_SIZE = (160, 240)

LAYOUTS = {
    "full": {
        "size": FULL_SIZE,
        "cost": (13, 12, 41, 41),
        "tier": (205, 13, 226, 41),
        "attack": (13, 327, 68, 349),
        "health": (172, 327, 227, 349),
        "small_font": 15,
        "stat_font": 15,
    },
    "compact": {
        "size": COMPACT_SIZE,
        "cost": (9, 8, 28, 28),
        "tier": (137, 9, 151, 28),
        "attack": (9, 218, 45, 233),
        "health": (115, 218, 151, 233),
        "small_font": 10,
        "stat_font": 10,
    },
}


def font(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT_PATH), max(1, size))


def alpha_trim(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    bounds = rgba.getchannel("A").getbbox()
    if not bounds:
        raise ValueError("Component has no visible alpha pixels")
    return rgba.crop(bounds)


def fit_component(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Contain an alpha-trimmed component in a transparent target box."""

    trimmed = alpha_trim(image)
    fitted = ImageOps.contain(trimmed, size, Image.Resampling.LANCZOS)
    result = Image.new("RGBA", size, (0, 0, 0, 0))
    result.alpha_composite(
        fitted,
        ((size[0] - fitted.width) // 2, (size[1] - fitted.height) // 2),
    )
    return result


def cost_component(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Favor the coin face; the generated fastening loop stays incidental."""

    trimmed = alpha_trim(image)
    top = round(trimmed.height * 0.15)
    trimmed = trimmed.crop((0, top, trimmed.width, trimmed.height))
    return fit_component(trimmed, size)


def tier_component(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Compress the long source bookmark into the short in-card tier tab."""

    trimmed = alpha_trim(image)
    return trimmed.resize(size, Image.Resampling.LANCZOS)


def nine_slice_horizontal(
    image: Image.Image,
    size: tuple[int, int],
    source_left_fraction: float,
    source_right_fraction: float,
    target_left_factor: float,
    target_right_factor: float,
) -> Image.Image:
    """Shorten a horizontal tag while preserving both icon/end caps."""

    width, height = size
    trimmed = alpha_trim(image)
    scaled_width = max(3, round(trimmed.width * height / trimmed.height))
    scaled = trimmed.resize((scaled_width, height), Image.Resampling.LANCZOS)

    source_left = max(1, round(scaled_width * source_left_fraction))
    source_right = max(1, round(scaled_width * source_right_fraction))
    target_left = min(round(height * target_left_factor), width // 3)
    target_right = min(round(height * target_right_factor), width // 3)
    target_center = max(1, width - target_left - target_right)

    left = scaled.crop((0, 0, source_left, height)).resize(
        (target_left, height),
        Image.Resampling.LANCZOS,
    )
    center = scaled.crop(
        (source_left, 0, scaled_width - source_right, height)
    ).resize((target_center, height), Image.Resampling.LANCZOS)
    right = scaled.crop(
        (scaled_width - source_right, 0, scaled_width, height)
    ).resize((target_right, height), Image.Resampling.LANCZOS)

    result = Image.new("RGBA", size, (0, 0, 0, 0))
    result.alpha_composite(left, (0, 0))
    result.alpha_composite(center, (target_left, 0))
    result.alpha_composite(right, (target_left + target_center, 0))
    return result


def load_components() -> dict[str, Image.Image]:
    return {
        name: Image.open(COMPONENTS / f"{name}.png").convert("RGBA")
        for name in ("cost-coin", "tier-bookmark", "attack-tag", "health-tag")
    }


def load_card_copy() -> dict:
    document = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    return next(
        card
        for card in document["minions"]
        if card["id"] == "undying_furnace_king"
    )


def rounded_mask(
    size: tuple[int, int],
    box: tuple[int, int, int, int],
    radius: int,
) -> Image.Image:
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).rounded_rectangle(box, radius=radius, fill=255)
    return mask


def draw_centered_text(
    draw: ImageDraw.ImageDraw,
    box: tuple[float, float, float, float],
    text: str,
    text_font: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int, int],
    stroke_fill: tuple[int, int, int, int] | None = None,
    stroke_width: int = 0,
) -> None:
    bounds = draw.textbbox(
        (0, 0),
        text,
        font=text_font,
        stroke_width=stroke_width,
    )
    width = bounds[2] - bounds[0]
    height = bounds[3] - bounds[1]
    x = box[0] + (box[2] - box[0] - width) / 2 - bounds[0]
    y = box[1] + (box[3] - box[1] - height) / 2 - bounds[1]
    draw.text(
        (x, y),
        text,
        font=text_font,
        fill=fill,
        stroke_fill=stroke_fill,
        stroke_width=stroke_width,
    )


def fit_number_font(
    draw: ImageDraw.ImageDraw,
    text: str,
    box: tuple[int, int, int, int],
    maximum: int,
    stroke_width: int = 1,
) -> ImageFont.FreeTypeFont:
    available_width = box[2] - box[0]
    available_height = box[3] - box[1]
    for size in range(maximum, 6, -1):
        candidate = font(size)
        bounds = draw.textbbox(
            (0, 0),
            text,
            font=candidate,
            stroke_width=stroke_width,
        )
        if (
            bounds[2] - bounds[0] <= available_width
            and bounds[3] - bounds[1] <= available_height
        ):
            return candidate
    return font(7)


def wrap_characters(
    draw: ImageDraw.ImageDraw,
    text: str,
    text_font: ImageFont.FreeTypeFont,
    max_width: int,
    max_lines: int,
) -> list[str]:
    lines: list[str] = []
    current = ""
    for character in text:
        candidate = current + character
        if current and draw.textlength(candidate, font=text_font) > max_width:
            lines.append(current)
            current = character
            if len(lines) == max_lines:
                break
        else:
            current = candidate
    if len(lines) < max_lines and current:
        lines.append(current)
    consumed = sum(len(line) for line in lines)
    if consumed < len(text) and lines:
        while (
            lines[-1]
            and draw.textlength(lines[-1] + "…", font=text_font) > max_width
        ):
            lines[-1] = lines[-1][:-1]
        lines[-1] += "…"
    return lines


def add_paper_grain(
    image: Image.Image,
    box: tuple[int, int, int, int],
    seed: int,
    count: int,
) -> None:
    rng = random.Random(seed)
    draw = ImageDraw.Draw(image, "RGBA")
    for _ in range(count):
        x = rng.randrange(box[0], box[2])
        y = rng.randrange(box[1], box[3])
        tone = rng.choice(
            (
                (113, 76, 39, 13),
                (255, 250, 221, 20),
                (153, 105, 54, 9),
            )
        )
        draw.point((x, y), fill=tone)


def compose_native_base() -> Image.Image:
    """Build art/panels and alpha-composite the exact native frame pixels."""

    frame = Image.open(FRAME_PATH).convert("RGBA")
    if frame.size != FRAME_SIZE:
        raise ValueError(f"Unexpected frame size: {frame.size}, expected {FRAME_SIZE}")

    art = Image.open(ART_PATH).convert("RGB")
    card = load_card_copy()
    canvas = Image.new("RGBA", FRAME_SIZE, (0, 0, 0, 0))

    art_box = (94, 99, 923, 918)
    art_crop = ImageOps.fit(
        art.crop((64, 120, 960, 1006)),
        (art_box[2] - art_box[0], art_box[3] - art_box[1]),
        Image.Resampling.LANCZOS,
        centering=(0.5, 0.46),
    ).convert("RGBA")
    art_mask = rounded_mask(FRAME_SIZE, art_box, 100)
    canvas.paste(art_crop, (art_box[0], art_box[1]), art_mask.crop(art_box))

    draw = ImageDraw.Draw(canvas, "RGBA")
    name_box = (101, 925, 916, 1049)
    info_box = (96, 1056, 921, 1484)
    draw.rounded_rectangle(
        name_box,
        radius=55,
        fill=(35, 42, 58, 250),
        outline=(111, 78, 48, 220),
        width=3,
    )
    draw.rounded_rectangle(
        info_box,
        radius=96,
        fill=(236, 221, 184, 255),
        outline=(154, 111, 63, 210),
        width=3,
    )
    add_paper_grain(canvas, info_box, seed=902, count=6800)

    # The source project frame is never redrawn: it is alpha-composited once,
    # at its own native size, with no resize or recoloring.
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
        "铸魂 · 嘲讽",
        font(28),
        (205, 169, 119, 255),
    )

    description = card["description"]
    description_font = font(34)
    description_box = (158, 1148, 859, 1390)
    lines = wrap_characters(
        draw,
        description,
        description_font,
        description_box[2] - description_box[0],
        4,
    )
    line_height = 49
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

    draw.line((184, 1119, 833, 1119), fill=(153, 108, 60, 128), width=2)
    draw_centered_text(
        draw,
        (190, 1078, 827, 1120),
        "护盾壁垒",
        font(29),
        (119, 76, 44, 255),
    )
    draw.line((184, 1414, 833, 1414), fill=(153, 108, 60, 128), width=2)
    draw_centered_text(
        draw,
        (190, 1417, 827, 1454),
        "数值组件四位数压力样板",
        font(24),
        (120, 83, 52, 220),
    )
    content.putalpha(
        ImageChops.multiply(
            content.getchannel("A"),
            ImageOps.invert(frame.getchannel("A")),
        )
    )
    canvas = Image.alpha_composite(canvas, content)
    return canvas


def component_box(
    canvas: Image.Image,
    source: Image.Image,
    box: tuple[int, int, int, int],
    kind: str,
) -> tuple[int, int, int, int]:
    width = box[2] - box[0]
    height = box[3] - box[1]
    if kind == "cost":
        piece = cost_component(source, (width, height))
    elif kind == "tier":
        piece = tier_component(source, (width, height))
    elif kind == "attack":
        piece = nine_slice_horizontal(
            source,
            (width, height),
            source_left_fraction=0.14,
            source_right_fraction=0.055,
            target_left_factor=0.66,
            target_right_factor=0.28,
        )
    elif kind == "health":
        piece = nine_slice_horizontal(
            source,
            (width, height),
            source_left_fraction=0.055,
            source_right_fraction=0.20,
            target_left_factor=0.28,
            target_right_factor=0.78,
        )
    else:
        raise ValueError(f"Unknown component kind: {kind}")
    canvas.alpha_composite(piece, (box[0], box[1]))
    return box


def add_numeric_components(
    card_image: Image.Image,
    layout: dict,
    components: dict[str, Image.Image],
    values: tuple[str, str, str, str] = ("3", "5", "9999", "1200"),
) -> Image.Image:
    canvas = card_image.convert("RGBA").copy()
    cost_box = component_box(
        canvas,
        components["cost-coin"],
        layout["cost"],
        "cost",
    )
    tier_box = component_box(
        canvas,
        components["tier-bookmark"],
        layout["tier"],
        "tier",
    )
    attack_box = component_box(
        canvas,
        components["attack-tag"],
        layout["attack"],
        "attack",
    )
    health_box = component_box(
        canvas,
        components["health-tag"],
        layout["health"],
        "health",
    )

    draw = ImageDraw.Draw(canvas, "RGBA")
    cost_font = fit_number_font(
        draw,
        values[0],
        cost_box,
        layout["small_font"],
    )
    draw_centered_text(
        draw,
        cost_box,
        values[0],
        cost_font,
        (249, 237, 206, 255),
        (63, 39, 22, 255),
        1,
    )

    tier_font = fit_number_font(
        draw,
        values[1],
        tier_box,
        layout["small_font"],
    )
    draw_centered_text(
        draw,
        tier_box,
        values[1],
        tier_font,
        (83, 53, 29, 255),
        (244, 224, 183, 220),
        1,
    )

    attack_slot = (
        attack_box[0] + round((attack_box[3] - attack_box[1]) * 0.62),
        attack_box[1] + 1,
        attack_box[2] - round((attack_box[3] - attack_box[1]) * 0.18),
        attack_box[3] - 1,
    )
    attack_font = fit_number_font(
        draw,
        values[2],
        attack_slot,
        layout["stat_font"],
    )
    draw_centered_text(
        draw,
        attack_slot,
        values[2],
        attack_font,
        (246, 235, 210, 255),
        (25, 22, 20, 255),
        1,
    )

    health_slot = (
        health_box[0] + round((health_box[3] - health_box[1]) * 0.18),
        health_box[1] + 1,
        health_box[2] - round((health_box[3] - health_box[1]) * 0.68),
        health_box[3] - 1,
    )
    health_font = fit_number_font(
        draw,
        values[3],
        health_slot,
        layout["stat_font"],
    )
    draw_centered_text(
        draw,
        health_slot,
        values[3],
        health_font,
        (255, 236, 216, 255),
        (57, 24, 22, 255),
        1,
    )
    return canvas


def make_card_outputs(
    native_base: Image.Image,
    components: dict[str, Image.Image],
) -> dict[str, Image.Image]:
    results: dict[str, Image.Image] = {}
    for key, layout in LAYOUTS.items():
        target = native_base.resize(layout["size"], Image.Resampling.LANCZOS)
        results[key] = add_numeric_components(target, layout, components)
    return results


def paper_canvas(size: tuple[int, int]) -> Image.Image:
    canvas = Image.new("RGBA", size, (236, 219, 181, 255))
    add_paper_grain(canvas, (0, 0, size[0], size[1]), seed=221, count=9500)
    return canvas


def draw_component_preview(
    canvas: Image.Image,
    center_x: int,
    box_y: int,
    kind: str,
    source: Image.Image,
    size: tuple[int, int],
    number: str,
    number_size: int,
) -> None:
    box = (
        center_x - size[0] // 2,
        box_y,
        center_x - size[0] // 2 + size[0],
        box_y + size[1],
    )
    component_box(canvas, source, box, kind)
    draw = ImageDraw.Draw(canvas, "RGBA")
    if kind in ("cost", "tier"):
        slot = box
    elif kind == "attack":
        slot = (
            box[0] + round(size[1] * 0.62),
            box[1] + 2,
            box[2] - round(size[1] * 0.18),
            box[3] - 2,
        )
    else:
        slot = (
            box[0] + round(size[1] * 0.18),
            box[1] + 2,
            box[2] - round(size[1] * 0.68),
            box[3] - 2,
        )
    number_font = fit_number_font(draw, number, slot, number_size, 1)
    if kind == "tier":
        fill = (83, 53, 29, 255)
        stroke = (244, 224, 183, 220)
    elif kind == "health":
        fill = (255, 236, 216, 255)
        stroke = (57, 24, 22, 255)
    else:
        fill = (246, 235, 210, 255)
        stroke = (39, 28, 21, 255)
    draw_centered_text(
        draw,
        slot,
        number,
        number_font,
        fill,
        stroke,
        1,
    )


def build_sheet(
    cards: dict[str, Image.Image],
    components: dict[str, Image.Image],
) -> Image.Image:
    sheet = paper_canvas((1536, 1024))
    draw = ImageDraw.Draw(sheet, "RGBA")
    ink = (70, 49, 32, 255)
    muted = (124, 88, 55, 230)
    rule = (151, 108, 63, 112)

    draw_centered_text(
        draw,
        (80, 30, 1456, 86),
        "轻量数值组件 · 组件条＋实际卡面",
        font(40),
        ink,
    )
    draw_centered_text(
        draw,
        (80, 82, 1456, 112),
        "项目原卡框逐像素合成｜数字运行时叠加｜9999 / 1200 四位数压力验证",
        font(19),
        muted,
    )

    centers = (210, 560, 955, 1320)
    panel_bounds = (
        (48, 126, 372, 344),
        (398, 126, 722, 344),
        (748, 126, 1162, 344),
        (1188, 126, 1490, 344),
    )
    for bounds in panel_bounds:
        draw.rounded_rectangle(
            bounds,
            radius=18,
            fill=(246, 232, 201, 120),
            outline=(151, 108, 63, 72),
            width=1,
        )

    draw_component_preview(
        sheet,
        centers[0],
        156,
        "cost",
        components["cost-coin"],
        (74, 78),
        "3",
        40,
    )
    draw_component_preview(
        sheet,
        centers[1],
        148,
        "tier",
        components["tier-bookmark"],
        (55, 88),
        "5",
        38,
    )
    draw_component_preview(
        sheet,
        centers[2],
        168,
        "attack",
        components["attack-tag"],
        (268, 66),
        "9999",
        42,
    )
    draw_component_preview(
        sheet,
        centers[3],
        168,
        "health",
        components["health-tag"],
        (242, 66),
        "1200",
        42,
    )

    component_meta = (
        ("费用 COST", "Full 28×29 · Compact 19×20"),
        ("等级 TIER", "Full 21×28 · Compact 14×19"),
        ("攻击 ATK", "低矮条 · 四位数向内扩展"),
        ("生命 HP", "低矮条 · 四位数向内扩展"),
    )
    for center_x, (label, detail) in zip(centers, component_meta):
        draw_centered_text(
            draw,
            (center_x - 150, 254, center_x + 150, 287),
            label,
            font(22),
            ink,
        )
        draw_centered_text(
            draw,
            (center_x - 165, 292, center_x + 165, 320),
            detail,
            font(16),
            muted,
        )

    draw.line((48, 372, 1488, 372), fill=rule, width=1)
    draw_centered_text(
        draw,
        (190, 386, 770, 425),
        "FULL 240×360",
        font(26),
        ink,
    )
    draw_centered_text(
        draw,
        (850, 436, 1428, 475),
        "COMPACT 160×240",
        font(26),
        ink,
    )

    full_preview = cards["full"].resize((360, 540), Image.Resampling.LANCZOS)
    compact_preview = cards["compact"].resize((320, 480), Image.Resampling.LANCZOS)
    sheet.alpha_composite(full_preview, (300, 438))
    sheet.alpha_composite(compact_preview, (980, 490))

    draw = ImageDraw.Draw(sheet, "RGBA")
    draw_centered_text(
        draw,
        (190, 978, 770, 1011),
        "费用 / 等级保持次级；攻血贴底边，不遮挡立绘与名称",
        font(17),
        muted,
    )
    draw_centered_text(
        draw,
        (850, 970, 1430, 1006),
        "Compact 仍保留 1 px 数字描边与四位数完整边界",
        font(17),
        muted,
    )
    return sheet


def validate_outputs(cards: dict[str, Image.Image]) -> None:
    if cards["full"].size != FULL_SIZE:
        raise ValueError(f"Full output has wrong size: {cards['full'].size}")
    if cards["compact"].size != COMPACT_SIZE:
        raise ValueError(f"Compact output has wrong size: {cards['compact'].size}")
    for name, image in cards.items():
        if image.getbbox() is None:
            raise ValueError(f"{name} output is fully transparent")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    native_base = compose_native_base()
    components = load_components()
    cards = make_card_outputs(native_base, components)
    validate_outputs(cards)

    native_base.save(OUT / "card-base-native-frame-exact-v0.2.png", optimize=True)
    cards["full"].save(OUT / "full-240x360-v0.2.png", optimize=True)
    cards["compact"].save(OUT / "compact-160x240-v0.2.png", optimize=True)

    sheet = build_sheet(cards, components)
    sheet.save(
        OUT / "component-strip-card-validation-measured-v0.2.png",
        optimize=True,
    )
    sheet.crop((0, 0, 1536, 373)).save(
        OUT / "component-strip-v0.2.png",
        optimize=True,
    )


if __name__ == "__main__":
    main()
