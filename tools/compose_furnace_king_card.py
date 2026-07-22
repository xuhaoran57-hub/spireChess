"""Export the Furnace King runtime art and deterministic card composites."""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parent.parent
UNITY_ART = ROOT / "sc/Assets/Art/Presentation"
COMPOSITES = ROOT / "ui-concepts/phase-9b/card-composites"
FONT_PATH = ROOT / "sc/Assets/Art/Fonts/NotoSansCJKsc-Regular.otf"
ART_SOURCE = ROOT / "sc/Temp/phase9b-card-composite/undying-furnace-king.png"
NORMAL_FRAME_SOURCE = (
    ROOT
    / "ui-concepts/phase-9b/card-frames/shared-card-frame-normal-alpha-master-v0.1.png"
)
GOLDEN_FRAME_SOURCE = (
    ROOT
    / "ui-concepts/phase-9b/card-frames/shared-card-frame-golden-alpha-master-v0.1.png"
)
CONFIG_PATH = ROOT / "sc/Assets/Resources/Configs/Json/minions.v0.1.json"
V01_NORMAL_COMPOSITE = (
    COMPOSITES / "undying-furnace-king-normal-composite-v0.1.png"
)


FULL = {
    "size": (240, 360),
    "frame": (6, 6, 228, 348),
    "art": (12, 12, 216, 184),
    "cost": (8, 8, 48, 48),
    "tier": (184, 8, 48, 48),
    "state": (60, 157, 120, 22),
    "name": (24, 181, 192, 32),
    "info": (12, 199, 216, 149),
    "race": (44, 215, 152, 18),
    "labels": (20, 235, 200, 20),
    "description": (12, 256, 216, 52),
    "attack": (8, 308, 44, 44),
    "health": (188, 308, 44, 44),
}

COMPACT = {
    "size": (160, 240),
    "frame": (4, 4, 152, 232),
    "art": (8, 8, 144, 112),
    "tier": (120, 6, 34, 34),
    "state": (42, 91, 76, 18),
    "name": (16, 108, 128, 26),
    "info": (8, 122, 144, 110),
    "race": (28, 136, 104, 14),
    "labels": (12, 154, 136, 16),
    "description": (12, 172, 136, 33),
    "attack": (6, 204, 32, 32),
    "health": (122, 204, 32, 32),
}


def rect(layout: dict, key: str, scale: float) -> tuple[int, int, int, int]:
    x, y, width, height = layout[key]
    left = round(x * scale)
    top = round(y * scale)
    return left, top, left + round(width * scale), top + round(height * scale)


def load_card() -> dict:
    document = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    return next(
        card
        for card in document["minions"]
        if card["id"] == "undying_furnace_king"
    )


def font(size: float) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT_PATH), max(1, round(size)))


def text_size(draw: ImageDraw.ImageDraw, value: str, text_font: ImageFont.FreeTypeFont):
    box = draw.textbbox((0, 0), value, font=text_font)
    return box[2] - box[0], box[3] - box[1]


def draw_centered(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    value: str,
    text_font: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int, int],
) -> None:
    bounds = draw.textbbox((0, 0), value, font=text_font)
    width = bounds[2] - bounds[0]
    height = bounds[3] - bounds[1]
    x = box[0] + (box[2] - box[0] - width) / 2 - bounds[0]
    y = box[1] + (box[3] - box[1] - height) / 2 - bounds[1]
    draw.text((x, y), value, font=text_font, fill=fill, stroke_width=1)


def draw_centered_stroked(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    value: str,
    text_font: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int, int],
    stroke_fill: tuple[int, int, int, int],
    stroke_width: int,
) -> None:
    bounds = draw.textbbox(
        (0, 0),
        value,
        font=text_font,
        stroke_width=stroke_width,
    )
    width = bounds[2] - bounds[0]
    height = bounds[3] - bounds[1]
    x = box[0] + (box[2] - box[0] - width) / 2 - bounds[0]
    y = box[1] + (box[3] - box[1] - height) / 2 - bounds[1]
    draw.text(
        (x, y),
        value,
        font=text_font,
        fill=fill,
        stroke_fill=stroke_fill,
        stroke_width=stroke_width,
    )


def fit_single_line(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    value: str,
    base_size: float,
    minimum_size: float,
    scale: float,
) -> ImageFont.FreeTypeFont:
    available = box[2] - box[0] - round(8 * scale)
    for logical_size in range(round(base_size), round(minimum_size) - 1, -1):
        candidate = font(logical_size * scale)
        if text_size(draw, value, candidate)[0] <= available:
            return candidate
    return font(minimum_size * scale)


def wrap_text(
    draw: ImageDraw.ImageDraw,
    value: str,
    text_font: ImageFont.FreeTypeFont,
    max_width: int,
    max_lines: int,
) -> list[str]:
    lines: list[str] = []
    current = ""
    for character in value:
        candidate = current + character
        if current and text_size(draw, candidate, text_font)[0] > max_width:
            lines.append(current)
            current = character
            if len(lines) == max_lines:
                break
        else:
            current = candidate
    if len(lines) < max_lines and current:
        lines.append(current)
    consumed = sum(len(line) for line in lines)
    if consumed < len(value) and lines:
        while lines[-1] and text_size(draw, lines[-1] + "…", text_font)[0] > max_width:
            lines[-1] = lines[-1][:-1]
        lines[-1] += "…"
    return lines


def overlay_color(base: Image.Image, color: tuple[int, int, int, int]) -> Image.Image:
    layer = Image.new("RGBA", base.size, color)
    return Image.alpha_composite(base, layer)


def make_stat_badge_mask(size: tuple[int, int]) -> Image.Image:
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)
    draw.ellipse((29, 1374, 158, 1503), fill=255)
    draw.polygon(
        (
            (868, 1373),
            (988, 1373),
            (988, 1440),
            (983, 1465),
            (974, 1485),
            (964, 1498),
            (947, 1512),
            (928, 1520),
            (909, 1512),
            (892, 1498),
            (882, 1485),
            (873, 1465),
            (868, 1440),
        ),
        fill=255,
    )
    return mask


def make_v04_golden(
    base: Image.Image,
    normal_frame: Image.Image,
    golden_frame: Image.Image,
) -> Image.Image:
    base_rgba = np.asarray(base.convert("RGBA")).astype(np.float32)
    normal_rgba = np.asarray(normal_frame.convert("RGBA")).astype(np.float32)
    golden_rgba = np.asarray(golden_frame.convert("RGBA")).astype(np.float32)
    frame_alpha = normal_rgba[:, :, 3:4] / 255.0

    # Dynamic stat badges sit above the frame. Protect their exact v0.1
    # geometry while replacing only the underlying frame material.
    badge_mask = make_stat_badge_mask(base.size)
    badge_alpha = np.asarray(badge_mask).astype(np.float32)[:, :, None] / 255.0
    frame_alpha *= 1.0 - badge_alpha

    output = base_rgba.copy()
    output[:, :, :3] += (
        golden_rgba[:, :, :3] - normal_rgba[:, :, :3]
    ) * frame_alpha
    output[:, :, :3] = np.clip(output[:, :, :3], 0, 255)
    image = Image.fromarray(output.astype(np.uint8), "RGBA")

    draw = ImageDraw.Draw(image, "RGBA")
    draw.rounded_rectangle(
        (108, 1135, 916, 1418),
        radius=12,
        fill=(19, 20, 23, 255),
    )
    description_font = font(48)
    description_lines = (
        "战斗开始：获得护盾。每当一个友方",
        "铸魂失去护盾，使另一个无护盾友方",
        "铸魂获得护盾。每场战斗最多触发 4",
        "次。",
    )
    y = 1150
    for line in description_lines:
        width, _ = text_size(draw, line, description_font)
        draw.text(
            ((1024 - width) / 2, y),
            line,
            font=description_font,
            fill=(235, 237, 240, 255),
        )
        y += 62

    # The description panel reaches the health badge in the flattened v0.1
    # composition. Restore both original badges before changing their values.
    image.paste(base.convert("RGBA"), (0, 0), badge_mask)
    draw = ImageDraw.Draw(image, "RGBA")

    draw.rectangle((68, 1402, 119, 1475), fill=(47, 24, 17, 255))
    draw_centered_stroked(
        draw,
        (48, 1390, 139, 1487),
        "12",
        font(68),
        (250, 241, 218, 255),
        (0, 0, 0, 230),
        4,
    )
    draw.rectangle((904, 1402, 953, 1475), fill=(45, 19, 20, 255))
    draw_centered_stroked(
        draw,
        (883, 1390, 974, 1487),
        "16",
        font(68),
        (255, 235, 221, 255),
        (0, 0, 0, 230),
        4,
    )
    return image


def compose(
    card: dict,
    art: Image.Image,
    frame: Image.Image,
    layout: dict,
    golden: bool,
) -> Image.Image:
    logical_width, logical_height = layout["size"]
    scale = 1024 / logical_width
    canvas = Image.new("RGBA", (1024, round(logical_height * scale)), (240, 178, 92, 255))
    canvas = overlay_color(canvas, (128, 61, 46, 112))

    art_box = rect(layout, "art", scale)
    art_crop = ImageOps.fit(
        art.convert("RGB"),
        (art_box[2] - art_box[0], art_box[3] - art_box[1]),
        Image.Resampling.LANCZOS,
        centering=(0.5, 0.44 if layout is FULL else 0.42),
    ).convert("RGBA")
    canvas.alpha_composite(art_crop, (art_box[0], art_box[1]))

    draw = ImageDraw.Draw(canvas, "RGBA")
    name_box = rect(layout, "name", scale)
    info_box = rect(layout, "info", scale)
    draw.rounded_rectangle(name_box, radius=round(7 * scale), fill=(16, 18, 22, 238))
    draw.rounded_rectangle(info_box, radius=round(8 * scale), fill=(14, 16, 20, 232))

    frame_box = rect(layout, "frame", scale)
    frame_layer = frame.resize(
        (frame_box[2] - frame_box[0], frame_box[3] - frame_box[1]),
        Image.Resampling.LANCZOS,
    )
    canvas.alpha_composite(frame_layer, (frame_box[0], frame_box[1]))

    draw = ImageDraw.Draw(canvas, "RGBA")

    if "cost" in layout:
        cost_box = rect(layout, "cost", scale)
        draw.rounded_rectangle(cost_box, radius=round(16 * scale), fill=(34, 105, 166, 248))
        draw_centered(draw, cost_box, "3", font(26 * scale), (245, 248, 252, 255))

    tier_box = rect(layout, "tier", scale)
    draw.rounded_rectangle(tier_box, radius=round(14 * scale), fill=(38, 26, 12, 248))
    draw_centered(
        draw,
        tier_box,
        "T5",
        font((24 if layout is FULL else 17) * scale),
        (255, 219, 125, 255),
    )

    name_font = fit_single_line(
        draw,
        name_box,
        card["name"],
        22 if layout is FULL else 16,
        18 if layout is FULL else 14,
        scale,
    )
    draw_centered(
        draw,
        name_box,
        card["name"],
        name_font,
        (255, 210, 88, 255) if golden else (244, 247, 250, 255),
    )

    race_box = rect(layout, "race", scale)
    draw_centered(
        draw,
        race_box,
        "铸魂",
        font((14 if layout is FULL else 11) * scale),
        (236, 168, 121, 255),
    )
    label_box = rect(layout, "labels", scale)
    draw_centered(
        draw,
        label_box,
        "盾链",
        font((12 if layout is FULL else 10) * scale),
        (174, 226, 255, 255),
    )

    description = card["goldenDescription" if golden else "description"]
    description_box = rect(layout, "description", scale)
    description_size = (11 if layout is FULL else 10) * scale
    description_font = font(description_size)
    max_lines = 4 if layout is FULL else 2
    lines = wrap_text(
        draw,
        description,
        description_font,
        description_box[2] - description_box[0] - round(10 * scale),
        max_lines,
    )
    line_height = round(description_size * 1.28)
    total_height = line_height * len(lines)
    y = description_box[1] + max(0, (description_box[3] - description_box[1] - total_height) // 2)
    for line in lines:
        width, _ = text_size(draw, line, description_font)
        draw.text(
            ((description_box[0] + description_box[2] - width) / 2, y),
            line,
            font=description_font,
            fill=(232, 234, 238, 255),
        )
        y += line_height

    if golden:
        state_box = rect(layout, "state", scale)
        badge_width = round((34 if layout is FULL else 28) * scale)
        badge = (
            round((state_box[0] + state_box[2] - badge_width) / 2),
            state_box[1],
            round((state_box[0] + state_box[2] + badge_width) / 2),
            state_box[3],
        )
        draw.rounded_rectangle(badge, radius=round(5 * scale), fill=(95, 59, 12, 224))
        draw_centered(
            draw,
            badge,
            "金色",
            font((11 if layout is FULL else 10) * scale),
            (255, 210, 88, 255),
        )

    stat_font = font((26 if layout is FULL else 20) * scale)
    for key, value, color in (
        ("attack", card["goldenAttack" if golden else "attack"], (151, 47, 41, 252)),
        ("health", card["goldenHealth" if golden else "health"], (47, 131, 72, 252)),
    ):
        stat_box = rect(layout, key, scale)
        draw.ellipse(stat_box, fill=color, outline=(245, 205, 115, 255), width=max(1, round(scale)))
        draw_centered(draw, stat_box, str(value), stat_font, (250, 250, 250, 255))

    return canvas


def main() -> None:
    card = load_card()
    art = Image.open(ART_SOURCE)
    normal_frame = Image.open(NORMAL_FRAME_SOURCE).convert("RGBA")
    golden_frame = Image.open(GOLDEN_FRAME_SOURCE).convert("RGBA")

    runtime_art = UNITY_ART / "Cards/Minions/ForgeSoul/card_minion_undying_furnace_king.png"
    runtime_normal = UNITY_ART / "UI/Common/card_frame_normal.png"
    runtime_golden = UNITY_ART / "UI/Common/card_frame_golden.png"
    runtime_art.parent.mkdir(parents=True, exist_ok=True)
    runtime_normal.parent.mkdir(parents=True, exist_ok=True)
    art.resize((1024, 1024), Image.Resampling.LANCZOS).save(runtime_art, optimize=True)
    normal_frame.save(runtime_normal, optimize=True)
    golden_frame.save(runtime_golden, optimize=True)

    COMPOSITES.mkdir(parents=True, exist_ok=True)
    outputs: dict[str, Image.Image] = {}
    for label, layout in (("full", FULL), ("compact", COMPACT)):
        outputs[f"normal-{label}"] = compose(card, art, normal_frame, layout, False)
        outputs[f"golden-{label}"] = compose(card, art, golden_frame, layout, True)

    for variant in ("normal", "golden"):
        master = outputs[f"{variant}-full"]
        master.save(
            COMPOSITES / f"undying-furnace-king-{variant}-runtime-composite-v0.2.png",
            optimize=True,
        )
        master.resize((240, 360), Image.Resampling.LANCZOS).save(
            COMPOSITES / f"undying-furnace-king-{variant}-240x360-v0.2.png",
            optimize=True,
        )
        outputs[f"{variant}-compact"].resize((160, 240), Image.Resampling.LANCZOS).save(
            COMPOSITES / f"undying-furnace-king-{variant}-160x240-v0.2.png",
            optimize=True,
        )

    normal_preview = outputs["normal-full"].resize((240, 360), Image.Resampling.LANCZOS)
    golden_preview = outputs["golden-full"].resize((240, 360), Image.Resampling.LANCZOS)
    comparison = Image.new("RGBA", (500, 360), (24, 25, 30, 255))
    comparison.alpha_composite(normal_preview, (0, 0))
    comparison.alpha_composite(golden_preview, (260, 0))
    comparison.save(
        COMPOSITES / "undying-furnace-king-runtime-switch-validation-v0.2.png",
        optimize=True,
    )
    normal_preview.convert("P", palette=Image.Palette.ADAPTIVE).save(
        COMPOSITES / "undying-furnace-king-runtime-switch-validation-v0.2.gif",
        save_all=True,
        append_images=[golden_preview.convert("P", palette=Image.Palette.ADAPTIVE)],
        duration=[900, 900],
        loop=0,
        disposal=2,
    )

    # v0.4 keeps the approved v0.1 visual hierarchy and changes only the
    # golden presentation data and frame material.
    v04_normal = Image.open(V01_NORMAL_COMPOSITE).convert("RGBA")
    v04_golden = make_v04_golden(v04_normal, normal_frame, golden_frame)
    for variant, master in (("normal", v04_normal), ("golden", v04_golden)):
        master.save(
            COMPOSITES
            / f"undying-furnace-king-{variant}-runtime-composite-v0.4.png",
            optimize=True,
        )
        master.resize((240, 360), Image.Resampling.LANCZOS).save(
            COMPOSITES / f"undying-furnace-king-{variant}-240x360-v0.4.png",
            optimize=True,
        )

    v04_normal_preview = v04_normal.resize((240, 360), Image.Resampling.LANCZOS)
    v04_golden_preview = v04_golden.resize((240, 360), Image.Resampling.LANCZOS)
    v04_comparison = Image.new("RGBA", (500, 360), (24, 25, 30, 255))
    v04_comparison.alpha_composite(v04_normal_preview, (0, 0))
    v04_comparison.alpha_composite(v04_golden_preview, (260, 0))
    v04_comparison.save(
        COMPOSITES / "undying-furnace-king-runtime-switch-validation-v0.4.png",
        optimize=True,
    )
    v04_normal_preview.convert("P", palette=Image.Palette.ADAPTIVE).save(
        COMPOSITES / "undying-furnace-king-runtime-switch-validation-v0.4.gif",
        save_all=True,
        append_images=[
            v04_golden_preview.convert("P", palette=Image.Palette.ADAPTIVE)
        ],
        duration=[900, 900],
        loop=0,
        disposal=2,
    )


if __name__ == "__main__":
    main()
