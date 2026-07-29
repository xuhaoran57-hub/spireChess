from __future__ import annotations

import argparse
import json
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageChops, ImageDraw, ImageFont


SIMULATOR_ROOT = Path(__file__).resolve().parent
PRODUCTION_ROOT = SIMULATOR_ROOT.parent
REPOSITORY_ROOT = PRODUCTION_ROOT.parents[2]
ROUND_ROOT = (
    PRODUCTION_ROOT / "validation-round-7-v0.3.2-formal-catalog"
)
SPEC_PATH = PRODUCTION_ROOT / "FORMAL-CATALOG-SPECS-v0.3.2.json"
FOCAL_PATH = SIMULATOR_ROOT / "focal-points.json"
UNITY_ASSET_ROOT = REPOSITORY_ROOT / "sc" / "Assets"
FONT_PATH = UNITY_ASSET_ROOT / "Art" / "Fonts" / "NotoSansCJKsc-Regular.otf"
NORMAL_FRAME_PATH = (
    UNITY_ASSET_ROOT
    / "Art"
    / "Presentation"
    / "UI"
    / "Common"
    / "card_frame_storybook_normal_v2.png"
)
GOLDEN_FRAME_PATH = (
    UNITY_ASSET_ROOT
    / "Art"
    / "Presentation"
    / "UI"
    / "Common"
    / "card_frame_storybook_golden_v2.png"
)
NUMERIC_ROOT = (
    UNITY_ASSET_ROOT / "Art" / "Presentation" / "UI" / "Card"
)
OUTPUT_ROOT = SIMULATOR_ROOT / "matrices"


GROUPS = (
    ("ForgeSoul", "铸魂"),
    ("WildSpirit", "荒灵"),
    ("Starbound", "星契"),
    ("Wayfarer", "旅团"),
    ("Spell", "法术"),
)

TIER_BACKGROUNDS = {
    1: (209, 204, 191, 255),
    2: (140, 199, 153, 255),
    3: (133, 173, 230, 255),
    4: (179, 148, 224, 255),
    5: (240, 179, 92, 255),
}

RACE_SKINS = {
    "ForgeSoul": (128, 61, 46, 112),
    "WildSpirit": (51, 122, 69, 112),
    "Starbound": (51, 87, 158, 112),
    "Wayfarer": (117, 97, 61, 112),
    "Spell": (77, 71, 92, 102),
}

RACE_PLATES = {
    "ForgeSoul": (122, 56, 36, 245),
    "WildSpirit": (56, 99, 51, 245),
    "Starbound": (51, 71, 133, 245),
    "Wayfarer": (107, 82, 51, 245),
    "Spell": (74, 66, 79, 240),
}

INK = (61, 46, 34, 255)
MUTED_INK = (100, 75, 55, 255)
PAPER = (233, 215, 174, 250)
NORMAL_NAME = (242, 245, 250, 255)
GOLDEN_NAME = (255, 210, 88, 255)
COST_TEXT = (249, 237, 206, 255)
TIER_TEXT = (83, 53, 29, 255)
ATTACK_TEXT = (246, 235, 210, 255)
HEALTH_TEXT = (255, 236, 216, 255)
SPELL_TEXT = (53, 79, 121, 255)


@dataclass(frozen=True)
class Rect:
    x: int
    y: int
    width: int
    height: int


@dataclass(frozen=True)
class Layout:
    root: Rect
    frame: Rect
    art: Rect
    state: Rect
    name: Rect
    info: Rect
    race: Rect
    description: Rect
    attack: Rect
    health: Rect
    attack_number: Rect
    health_number: Rect
    footer: Rect
    cost: Rect
    tier: Rect


def layout_for(mode: str) -> Layout:
    if mode == "full":
        return Layout(
            root=Rect(0, 0, 240, 360),
            frame=Rect(6, 6, 228, 348),
            art=Rect(12, 12, 216, 184),
            state=Rect(44, 157, 152, 22),
            name=Rect(24, 181, 192, 32),
            info=Rect(12, 199, 216, 149),
            race=Rect(36, 232, 168, 24),
            description=Rect(30, 256, 180, 64),
            attack=Rect(10, 321, 68, 30),
            health=Rect(162, 321, 68, 30),
            attack_number=Rect(17, 2, 46, 26),
            health_number=Rect(5, 2, 46, 26),
            footer=Rect(80, 332, 80, 16),
            cost=Rect(13, 12, 28, 29),
            tier=Rect(198, 9, 32, 40),
        )
    return Layout(
        root=Rect(0, 0, 160, 240),
        frame=Rect(4, 4, 152, 232),
        art=Rect(8, 8, 144, 112),
        state=Rect(28, 91, 104, 18),
        name=Rect(16, 108, 128, 26),
        info=Rect(8, 122, 144, 110),
        race=Rect(24, 152, 112, 19),
        description=Rect(20, 172, 120, 33),
        attack=Rect(7, 213, 46, 21),
        health=Rect(107, 213, 46, 21),
        attack_number=Rect(11, 1, 31, 19),
        health_number=Rect(3, 1, 31, 19),
        footer=Rect(55, 220, 50, 13),
        cost=Rect(9, 8, 19, 20),
        tier=Rect(132, 6, 22, 28),
    )


@lru_cache(maxsize=None)
def font(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT_PATH), size=size)


def scaled_image(path: Path, size: tuple[int, int]) -> Image.Image:
    return Image.open(path).convert("RGBA").resize(
        size,
        Image.Resampling.LANCZOS,
    )


def tint(image: Image.Image, color: tuple[int, int, int, int]) -> Image.Image:
    solid = Image.new("RGBA", image.size, color)
    return ImageChops.multiply(image, solid)


def composite_color(
    target: Image.Image,
    rect: Rect,
    color: tuple[int, int, int, int],
) -> None:
    layer = Image.new("RGBA", (rect.width, rect.height), color)
    target.alpha_composite(layer, (rect.x, rect.y))


def fit_unity_art(
    source: Image.Image,
    viewport: Rect,
    focal_point_y: float,
) -> Image.Image:
    source = source.convert("RGB")
    source_aspect = source.width / source.height
    viewport_aspect = viewport.width / viewport.height
    if source_aspect >= viewport_aspect:
        display_height = viewport.height
        display_width = round(display_height * source_aspect)
        resized = source.resize(
            (display_width, display_height),
            Image.Resampling.LANCZOS,
        )
        left = max(0, (display_width - viewport.width) // 2)
        return resized.crop(
            (left, 0, left + viewport.width, viewport.height)
        ).convert("RGBA")

    display_width = viewport.width
    display_height = round(display_width / source_aspect)
    resized = source.resize(
        (display_width, display_height),
        Image.Resampling.LANCZOS,
    )
    overflow = max(0, display_height - viewport.height)
    top = round(max(0.0, min(1.0, focal_point_y)) * overflow)
    return resized.crop(
        (0, top, viewport.width, top + viewport.height)
    ).convert("RGBA")


def text_width(draw: ImageDraw.ImageDraw, value: str, chosen_font) -> float:
    box = draw.textbbox((0, 0), value, font=chosen_font)
    return box[2] - box[0]


def fit_single_line(
    draw: ImageDraw.ImageDraw,
    value: str,
    maximum_width: int,
    base_size: int,
    minimum_size: int,
) -> tuple[str, ImageFont.FreeTypeFont]:
    normalized = " ".join((value or "").split())
    for size in range(base_size, minimum_size - 1, -1):
        chosen = font(size)
        if text_width(draw, normalized, chosen) <= maximum_width:
            return normalized, chosen
    chosen = font(minimum_size)
    suffix = "…"
    candidate = normalized
    while candidate and text_width(
        draw,
        candidate + suffix,
        chosen,
    ) > maximum_width:
        candidate = candidate[:-1]
    return candidate + suffix, chosen


def wrap_text(
    draw: ImageDraw.ImageDraw,
    value: str,
    chosen_font,
    maximum_width: int,
) -> list[str]:
    lines: list[str] = []
    current = ""
    for character in " ".join((value or "").split()):
        candidate = current + character
        if current and text_width(draw, candidate, chosen_font) > maximum_width:
            lines.append(current)
            current = character
        else:
            current = candidate
    if current:
        lines.append(current)
    return lines


def fit_multiline(
    draw: ImageDraw.ImageDraw,
    value: str,
    maximum_width: int,
    maximum_height: int,
    maximum_lines: int,
    base_size: int,
    minimum_size: int,
    ellipsize: bool,
) -> tuple[list[str], ImageFont.FreeTypeFont]:
    normalized = " ".join((value or "").split())
    for size in range(base_size, minimum_size - 1, -1):
        chosen = font(size)
        lines = wrap_text(draw, normalized, chosen, maximum_width)
        height_limited_lines = max(1, maximum_height // round(size * 1.28))
        visible_lines = min(maximum_lines, height_limited_lines)
        if len(lines) <= visible_lines:
            return lines, chosen

    chosen = font(minimum_size)
    lines = wrap_text(draw, normalized, chosen, maximum_width)
    height_limited_lines = max(
        1,
        maximum_height // round(minimum_size * 1.28),
    )
    visible_line_count = min(maximum_lines, height_limited_lines)
    if not ellipsize or len(lines) <= visible_line_count:
        return lines[:visible_line_count], chosen

    visible = lines[:visible_line_count]
    last = visible[-1]
    while last and text_width(
        draw,
        last + "…",
        chosen,
    ) > maximum_width:
        last = last[:-1]
    visible[-1] = last + "…"
    return visible, chosen


def draw_centered_text(
    draw: ImageDraw.ImageDraw,
    rect: Rect,
    value: str,
    chosen_font,
    fill: tuple[int, int, int, int],
) -> None:
    box = draw.textbbox((0, 0), value, font=chosen_font)
    width = box[2] - box[0]
    height = box[3] - box[1]
    x = rect.x + (rect.width - width) / 2
    y = rect.y + (rect.height - height) / 2 - box[1]
    draw.text((x, y), value, font=chosen_font, fill=fill)


def card_group(spec: dict) -> str:
    return "Spell" if spec["kind"] == "Spell" else spec["race"]


def card_type_line(spec: dict) -> str:
    if spec["kind"] == "Spell":
        return "法术 · 商店法术"
    return dict(GROUPS).get(spec["race"], spec["race"])


def has_shield(spec: dict) -> bool:
    return "Shield" in spec.get("keywords", []) or "shield" in spec.get(
        "tags",
        [],
    )


class CardRenderer:
    def __init__(self) -> None:
        self.normal_frame = Image.open(NORMAL_FRAME_PATH).convert("RGBA")
        self.golden_frame = Image.open(GOLDEN_FRAME_PATH).convert("RGBA")
        self.cost_badge = Image.open(
            NUMERIC_ROOT / "card_cost_coin_v1.png"
        ).convert("RGBA")
        self.tier_badge = Image.open(
            NUMERIC_ROOT / "card_tier_bookmark_v1.png"
        ).convert("RGBA")
        self.attack_badge = Image.open(
            NUMERIC_ROOT / "card_attack_tag_v1.png"
        ).convert("RGBA")
        self.health_badge = Image.open(
            NUMERIC_ROOT / "card_health_tag_v1.png"
        ).convert("RGBA")

    def render(
        self,
        spec: dict,
        mode: str,
        golden_requested: bool,
        focal_point_y: float,
    ) -> Image.Image:
        layout = layout_for(mode)
        group = card_group(spec)
        is_minion = spec["kind"] == "Minion"
        is_golden = golden_requested and is_minion
        tier = spec["tier"]
        canvas = Image.new(
            "RGBA",
            (layout.root.width, layout.root.height),
            TIER_BACKGROUNDS.get(tier, TIER_BACKGROUNDS[1]),
        )
        composite_color(canvas, layout.root, RACE_SKINS[group])

        art_path = ROUND_ROOT / spec["artFile"]
        art = fit_unity_art(
            Image.open(art_path),
            layout.art,
            focal_point_y,
        )
        canvas.alpha_composite(art, (layout.art.x, layout.art.y))

        composite_color(canvas, layout.info, PAPER)
        composite_color(canvas, layout.name, RACE_PLATES[group])

        frame_source = self.golden_frame if is_golden else self.normal_frame
        frame = frame_source.resize(
            (layout.frame.width, layout.frame.height),
            Image.Resampling.LANCZOS,
        )
        if is_golden:
            frame = tint(frame, (255, 210, 112, 255))
        elif not is_minion:
            frame = tint(frame, (200, 217, 244, 255))
        canvas.alpha_composite(frame, (layout.frame.x, layout.frame.y))

        draw = ImageDraw.Draw(canvas)
        name_value, name_font = fit_single_line(
            draw,
            spec["name"],
            layout.name.width - 8,
            22 if mode == "full" else 16,
            18 if mode == "full" else 14,
        )
        draw_centered_text(
            draw,
            layout.name,
            name_value,
            name_font,
            GOLDEN_NAME if is_golden else NORMAL_NAME,
        )

        race_value, race_font = fit_single_line(
            draw,
            card_type_line(spec),
            layout.race.width,
            16 if mode == "full" else 13,
            13,
        )
        draw_centered_text(
            draw,
            layout.race,
            race_value,
            race_font,
            MUTED_INK,
        )

        description = (
            spec.get("goldenDescription") or spec["description"]
            if is_golden
            else spec["description"]
        )
        maximum_lines = (
            5
            if mode == "full"
            else (3 if is_minion else 4)
        )
        description_lines, description_font = fit_multiline(
            draw,
            description,
            layout.description.width,
            layout.description.height,
            maximum_lines,
            14 if mode == "full" else 13,
            11 if mode == "full" else 12,
            ellipsize=True,
        )
        line_height = round(description_font.size * 1.28)
        text_y = layout.description.y
        for line in description_lines:
            draw.text(
                (layout.description.x, text_y),
                line,
                font=description_font,
                fill=INK,
            )
            text_y += line_height

        cost = scaled_image(
            NUMERIC_ROOT / "card_cost_coin_v1.png",
            (layout.cost.width, layout.cost.height),
        )
        canvas.alpha_composite(cost, (layout.cost.x, layout.cost.y))
        draw = ImageDraw.Draw(canvas)
        draw_centered_text(
            draw,
            layout.cost,
            str(spec.get("cost", 3) if not is_minion else 3),
            font(18 if mode == "full" else 13),
            COST_TEXT,
        )

        tier_badge = self.tier_badge.resize(
            (layout.tier.width, layout.tier.height),
            Image.Resampling.LANCZOS,
        )
        canvas.alpha_composite(tier_badge, (layout.tier.x, layout.tier.y))
        draw = ImageDraw.Draw(canvas)
        draw_centered_text(
            draw,
            layout.tier,
            str(tier),
            font(22 if mode == "full" else 16),
            TIER_TEXT,
        )

        if is_minion:
            attack = (
                spec.get("goldenAttack", spec["attack"])
                if is_golden
                else spec["attack"]
            )
            health = (
                spec.get("goldenHealth", spec["health"])
                if is_golden
                else spec["health"]
            )
            attack_badge = self.attack_badge.resize(
                (layout.attack.width, layout.attack.height),
                Image.Resampling.LANCZOS,
            )
            health_badge = self.health_badge.resize(
                (layout.health.width, layout.health.height),
                Image.Resampling.LANCZOS,
            )
            canvas.alpha_composite(
                attack_badge,
                (layout.attack.x, layout.attack.y),
            )
            canvas.alpha_composite(
                health_badge,
                (layout.health.x, layout.health.y),
            )
            draw = ImageDraw.Draw(canvas)
            attack_text_rect = Rect(
                layout.attack.x + layout.attack_number.x,
                layout.attack.y + layout.attack_number.y,
                layout.attack_number.width,
                layout.attack_number.height,
            )
            health_text_rect = Rect(
                layout.health.x + layout.health_number.x,
                layout.health.y + layout.health_number.y,
                layout.health_number.width,
                layout.health_number.height,
            )
            stat_font = font(22 if mode == "full" else 16)
            draw_centered_text(
                draw,
                attack_text_rect,
                str(attack),
                stat_font,
                ATTACK_TEXT,
            )
            draw_centered_text(
                draw,
                health_text_rect,
                str(health),
                stat_font,
                HEALTH_TEXT,
            )
            if has_shield(spec):
                state_font = font(11 if mode == "full" else 10)
                draw_centered_text(
                    draw,
                    layout.state,
                    "护盾",
                    state_font,
                    (104, 199, 255, 255),
                )
        else:
            draw_centered_text(
                draw,
                layout.footer,
                "商店法术",
                font(12 if mode == "full" else 10),
                SPELL_TEXT,
            )

        return canvas.convert("RGB")


def ordered_cards(specs: Iterable[dict]) -> list[dict]:
    cards = list(specs)
    ordered: list[dict] = []
    for group, _ in GROUPS:
        group_cards = [
            card for card in cards if card_group(card) == group
        ]
        ordered.extend(sorted(group_cards, key=lambda card: (card["tier"], card["id"])))
    return ordered


def render_matrix(
    renderer: CardRenderer,
    cards: list[dict],
    focal_points: dict[str, float],
    mode: str,
    golden: bool,
) -> Image.Image:
    layout = layout_for(mode)
    card_width = layout.root.width
    card_height = layout.root.height
    margin_x = 100
    header_height = 104
    footer_height = 54
    horizontal_gap = (1920 - 2 * margin_x - 5 * card_width) // 4
    vertical_gap = 28 if mode == "compact" else 36
    height = (
        header_height
        + 3 * card_height
        + 2 * vertical_gap
        + footer_height
    )
    matrix = Image.new("RGB", (1920, height), (204, 217, 212))
    draw = ImageDraw.Draw(matrix)
    title = (
        ("金色" if golden else "普通")
        + " · "
        + ("紧凑 160×240" if mode == "compact" else "完整 240×360")
        + " · 15 张正式卡"
    )
    draw.text((60, 18), title, font=font(28), fill=(51, 36, 23))
    subtitle = (
        "铸魂 / 荒灵 / 星契 / 旅团 / 法术；"
        + ("法术没有金色形态，保持普通卡框" if golden else "离线 CardView 几何近似")
    )
    draw.text((60, 58), subtitle, font=font(15), fill=(89, 82, 73))

    grouped = {
        group: [card for card in cards if card_group(card) == group]
        for group, _ in GROUPS
    }
    for column, (group, group_title) in enumerate(GROUPS):
        x = margin_x + column * (card_width + horizontal_gap)
        draw.text(
            (x, 82),
            group_title,
            font=font(18),
            fill=(100, 69, 41),
        )
        for row, spec in enumerate(grouped[group]):
            y = header_height + row * (card_height + vertical_gap)
            card_image = renderer.render(
                spec,
                mode,
                golden,
                focal_points.get(spec["id"], 0.5),
            )
            matrix.paste(card_image, (x, y))

    footer_y = height - footer_height + 15
    draw.text(
        (60, footer_y),
        "重点检查：主体裁切、名称/规则密度、种族色、金框亮度与法术字段。",
        font=font(14),
        fill=(89, 82, 73),
    )
    return matrix


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=OUTPUT_ROOT,
    )
    args = parser.parse_args()

    document = json.loads(SPEC_PATH.read_text(encoding="utf-8"))
    cards = ordered_cards(document["cards"])
    focal_points = json.loads(FOCAL_PATH.read_text(encoding="utf-8"))
    if len(cards) != 15:
        raise ValueError(f"Expected 15 cards, got {len(cards)}")
    if set(focal_points) != {card["id"] for card in cards}:
        raise ValueError("Focal-point ids do not match formal catalog ids.")

    args.output_dir.mkdir(parents=True, exist_ok=True)
    renderer = CardRenderer()
    for mode in ("compact", "full"):
        for golden in (False, True):
            variant = "golden" if golden else "normal"
            output_path = args.output_dir / f"matrix-{variant}-{mode}.png"
            render_matrix(
                renderer,
                cards,
                focal_points,
                mode,
                golden,
            ).save(output_path, optimize=True)
            print(output_path)


if __name__ == "__main__":
    main()
