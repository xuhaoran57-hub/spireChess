from __future__ import annotations

import importlib.util
import json
import sys
from functools import lru_cache
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parent
PRODUCTION_ROOT = ROOT.parent
REPOSITORY_ROOT = PRODUCTION_ROOT.parents[2]
CARD_SIMULATOR_ROOT = PRODUCTION_ROOT / "offline-card-simulator-v0.1"
CATALOG_ROOT = (
    PRODUCTION_ROOT / "validation-round-7-v0.3.2-formal-catalog"
)
SPEC_PATH = PRODUCTION_ROOT / "FORMAL-CATALOG-SPECS-v0.3.2.json"
FOCAL_PATH = CARD_SIMULATOR_ROOT / "focal-points.json"
SCENE_ROOT = ROOT / "scenes"
CARD_OUTPUT_ROOT = ROOT / "cards"

SHOP_BACKGROUND = (
    PRODUCTION_ROOT
    / "validation-round-2"
    / "backgrounds"
    / "background-shop-v0.1.png"
)
BATTLE_BACKGROUND = (
    PRODUCTION_ROOT
    / "ab-production-v0.1"
    / "battle-backdrop-new-light.png"
)
FONT_PATH = (
    REPOSITORY_ROOT
    / "sc"
    / "Assets"
    / "Art"
    / "Fonts"
    / "NotoSansCJKsc-Regular.otf"
)
STANDEE_ROOT = (
    REPOSITORY_ROOT
    / "sc"
    / "Assets"
    / "Art"
    / "Presentation"
    / "UI"
    / "Battle"
    / "Standee"
)

SCREEN_SIZE = (1920, 1080)
INK = (58, 43, 31, 255)
MUTED = (104, 88, 70, 255)
PAPER = (248, 239, 218, 228)
PAPER_SOLID = (248, 239, 218, 255)
PANEL_BORDER = (151, 113, 67, 205)
BLUE_INK = (41, 79, 91, 255)
GOLD = (235, 171, 59, 255)
GREEN = (76, 123, 84, 255)
RED = (151, 70, 55, 255)


def load_card_renderer_module():
    source = CARD_SIMULATOR_ROOT / "render_matrices.py"
    spec = importlib.util.spec_from_file_location(
        "light_storybook_card_renderer",
        source,
    )
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load {source}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


CARD_RENDERER = load_card_renderer_module()


@lru_cache(maxsize=None)
def font(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT_PATH), size=size)


def cover(source: Image.Image, size: tuple[int, int]) -> Image.Image:
    source = source.convert("RGB")
    scale = max(size[0] / source.width, size[1] / source.height)
    resized = source.resize(
        (round(source.width * scale), round(source.height * scale)),
        Image.Resampling.LANCZOS,
    )
    left = (resized.width - size[0]) // 2
    top = (resized.height - size[1]) // 2
    return resized.crop(
        (left, top, left + size[0], top + size[1])
    ).convert("RGBA")


def center_crop(source: Image.Image, size: tuple[int, int]) -> Image.Image:
    return cover(source, size)


def rounded_panel(
    canvas: Image.Image,
    box: tuple[int, int, int, int],
    fill: tuple[int, int, int, int] = PAPER,
    radius: int = 18,
    outline: tuple[int, int, int, int] = PANEL_BORDER,
    width: int = 2,
) -> None:
    overlay = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    draw.rounded_rectangle(
        box,
        radius=radius,
        fill=fill,
        outline=outline,
        width=width,
    )
    canvas.alpha_composite(overlay)


def draw_text(
    canvas: Image.Image,
    position: tuple[int, int],
    value: str,
    size: int,
    fill: tuple[int, int, int, int] = INK,
    anchor: str | None = None,
) -> None:
    ImageDraw.Draw(canvas).text(
        position,
        value,
        font=font(size),
        fill=fill,
        anchor=anchor,
    )


def paste_with_shadow(
    canvas: Image.Image,
    item: Image.Image,
    position: tuple[int, int],
    shadow_radius: int = 10,
    shadow_offset: tuple[int, int] = (0, 7),
) -> None:
    item = item.convert("RGBA")
    alpha = item.getchannel("A")
    shadow = Image.new("RGBA", item.size, (48, 35, 25, 0))
    shadow.putalpha(alpha.filter(ImageFilter.GaussianBlur(shadow_radius)))
    canvas.alpha_composite(
        shadow,
        (
            position[0] + shadow_offset[0],
            position[1] + shadow_offset[1],
        ),
    )
    canvas.alpha_composite(item, position)


def tint(image: Image.Image, color: tuple[int, int, int, int]) -> Image.Image:
    return ImageChops.multiply(
        image.convert("RGBA"),
        Image.new("RGBA", image.size, color),
    )


def fit_portrait(
    source: Image.Image,
    size: tuple[int, int],
    crop_mode: str,
) -> Image.Image:
    if crop_mode == "runtime-stretch":
        return source.convert("RGBA").resize(size, Image.Resampling.LANCZOS)
    return center_crop(source, size)


class StandeeRenderer:
    def __init__(self, bright_shield: bool = False) -> None:
        self.normal_frame = Image.open(
            STANDEE_ROOT / "standee_frame_silver_v1.png"
        ).convert("RGBA")
        self.golden_frame = Image.open(
            STANDEE_ROOT / "standee_frame.png"
        ).convert("RGBA")
        self.attack = Image.open(
            STANDEE_ROOT / "attack_medallion.png"
        ).convert("RGBA")
        self.health = Image.open(
            STANDEE_ROOT / "health_medallion.png"
        ).convert("RGBA")
        self.shield = Image.open(
            STANDEE_ROOT
            / (
                "shield_overlay_bright_storybook_v1.png"
                if bright_shield
                else "shield_overlay_screen.png"
            )
        ).convert("RGBA")
        self.taunt = Image.open(
            STANDEE_ROOT / "taunt_base.png"
        ).convert("RGBA")
        self.deathrattle = Image.open(
            STANDEE_ROOT / "deathrattle_seal.png"
        ).convert("RGBA")
        self.splash = Image.open(
            STANDEE_ROOT / "splash_mark.png"
        ).convert("RGBA")

    def render(
        self,
        card: dict,
        golden: bool,
        crop_mode: str,
    ) -> Image.Image:
        canvas = Image.new("RGBA", (160, 240), (0, 0, 0, 0))
        keywords = set(card.get("keywords", []))
        tags = set(card.get("tags", []))

        if "Taunt" in keywords:
            taunt = self.taunt.resize((176, 35), Image.Resampling.LANCZOS)
            canvas.alpha_composite(taunt, (-8, 205))

        source = Image.open(CATALOG_ROOT / card["artFile"])
        portrait = fit_portrait(source, (120, 192), crop_mode)
        canvas.alpha_composite(portrait, (20, 14))

        if "Shield" in keywords or "shield" in tags:
            shield = self.shield.resize((132, 222), Image.Resampling.LANCZOS)
            shield.putalpha(
                shield.getchannel("A").point(lambda value: round(value * 0.78))
            )
            canvas.alpha_composite(shield, (14, 8))

        frame_source = self.golden_frame if golden else self.normal_frame
        frame = frame_source.resize((132, 228), Image.Resampling.LANCZOS)
        if golden:
            frame = tint(frame, (255, 230, 158, 255))
        canvas.alpha_composite(frame, (14, 5))

        if "Deathrattle" in keywords:
            seal = self.deathrattle.resize((56, 56), Image.Resampling.LANCZOS)
            canvas.alpha_composite(seal, (52, -4))

        if "Cleave" in keywords:
            mark = self.splash.resize((26, 44), Image.Resampling.LANCZOS)
            canvas.alpha_composite(mark, (48, 155))

        attack = self.attack.resize((56, 56), Image.Resampling.LANCZOS)
        health = self.health.resize((56, 56), Image.Resampling.LANCZOS)
        canvas.alpha_composite(attack, (1, 184))
        canvas.alpha_composite(health, (103, 184))
        draw = ImageDraw.Draw(canvas)
        attack_value = card.get(
            "goldenAttack" if golden else "attack",
            card.get("attack", 0),
        )
        health_value = card.get(
            "goldenHealth" if golden else "health",
            card.get("health", 0),
        )
        for value, center in (
            (attack_value, (29, 211)),
            (health_value, (131, 211)),
        ):
            draw.text(
                center,
                str(value),
                font=font(24),
                fill=(255, 248, 230, 255),
                stroke_width=1,
                stroke_fill=(47, 31, 23, 255),
                anchor="mm",
            )
        return canvas


def load_cards() -> tuple[list[dict], dict[str, float]]:
    document = json.loads(SPEC_PATH.read_text(encoding="utf-8"))
    cards = CARD_RENDERER.ordered_cards(document["cards"])
    focal_points = json.loads(FOCAL_PATH.read_text(encoding="utf-8"))
    return cards, focal_points


def card_map(cards: list[dict]) -> dict[str, dict]:
    return {card["id"]: card for card in cards}


def export_card_assets(
    renderer,
    cards: list[dict],
    focal_points: dict[str, float],
) -> None:
    for mode in ("compact", "full"):
        for variant, golden in (("normal", False), ("golden", True)):
            output = CARD_OUTPUT_ROOT / f"{variant}-{mode}"
            output.mkdir(parents=True, exist_ok=True)
            for card in cards:
                renderer.render(
                    card,
                    mode,
                    golden,
                    focal_points[card["id"]],
                ).save(output / f"{card['id']}.png", optimize=True)


def render_shop(
    renderer,
    cards: list[dict],
    focal_points: dict[str, float],
) -> Image.Image:
    cards_by_id = card_map(cards)
    canvas = cover(Image.open(SHOP_BACKGROUND), SCREEN_SIZE)
    veil = Image.new("RGBA", SCREEN_SIZE, (247, 241, 224, 32))
    canvas.alpha_composite(veil)

    rounded_panel(canvas, (30, 18, 1890, 96), fill=(245, 235, 213, 238))
    draw_text(canvas, (58, 40), "商店阶段", 28, BLUE_INK)
    draw_text(canvas, (245, 45), "第 7 回合", 20)
    draw_text(canvas, (405, 45), "金币 8", 20, (150, 96, 25, 255))
    draw_text(canvas, (535, 45), "酒馆等级 5", 20, GREEN)
    draw_text(canvas, (720, 45), "升级费用 5", 20, (98, 75, 140, 255))
    draw_text(
        canvas,
        (1860, 45),
        "购买、使用手牌或调整阵容",
        18,
        MUTED,
        anchor="ra",
    )

    rounded_panel(canvas, (30, 108, 1610, 490))
    rounded_panel(canvas, (30, 500, 1610, 768))
    rounded_panel(canvas, (30, 778, 1610, 1050))
    rounded_panel(canvas, (1655, 108, 1890, 1050), fill=(242, 231, 205, 242))

    draw_text(canvas, (52, 116), "商品区 · 4 名随从 + 1 张法术", 18, MUTED)
    draw_text(canvas, (52, 508), "战斗区 · 5/5", 18, MUTED)
    draw_text(canvas, (52, 786), "手牌 · 5/5", 18, MUTED)

    offer_ids = [
        "young_deer_spirit",
        "glimmer_mage",
        "resonance_bell_guard",
        "traveling_physician",
        "temporary_ward",
    ]
    offer_golden = {"glimmer_mage"}
    offer_width = 5 * 240 + 4 * 22
    offer_x = 30 + (1580 - offer_width) // 2
    for index, card_id in enumerate(offer_ids):
        card = cards_by_id[card_id]
        golden = card_id in offer_golden
        image = renderer.render(
            card,
            "full",
            golden,
            focal_points[card_id],
        )
        paste_with_shadow(canvas, image, (offer_x + index * 262, 126))

    battle_ids = [
        "forge_soul_shield_squire",
        "rootbound_soul_guide",
        "fate_track_recorder",
        "mirrorsteel_duelist",
        "ancient_mountain_spirit",
    ]
    battle_golden = {"mirrorsteel_duelist"}
    row_width = 5 * 160 + 4 * 34
    row_x = 30 + (1580 - row_width) // 2
    for index, card_id in enumerate(battle_ids):
        image = renderer.render(
            cards_by_id[card_id],
            "compact",
            card_id in battle_golden,
            focal_points[card_id],
        )
        paste_with_shadow(canvas, image, (row_x + index * 194, 526), 7, (0, 5))

    hand_ids = [
        "undying_furnace_king",
        "old_tower_guide",
        "moonwheel_dispatcher",
        "starlight_rebate",
        "legendary_recruitment",
    ]
    hand_golden = {"undying_furnace_king"}
    for index, card_id in enumerate(hand_ids):
        image = renderer.render(
            cards_by_id[card_id],
            "compact",
            card_id in hand_golden,
            focal_points[card_id],
        )
        paste_with_shadow(canvas, image, (row_x + index * 194, 806), 7, (0, 5))

    draw_text(canvas, (1678, 130), "当前选择", 16, MUTED)
    draw_text(canvas, (1678, 165), "微光术士（金色）", 22, BLUE_INK)
    draw_text(canvas, (1678, 205), "星契 · T1 · 3 费", 16, MUTED)
    ImageDraw.Draw(canvas).line(
        (1678, 245, 1868, 245),
        fill=(159, 130, 91, 170),
        width=1,
    )
    detail_lines = [
        "使用法术后永久成长，",
        "首次使用法术后，",
        "下一场战斗获得护盾。",
    ]
    for index, line in enumerate(detail_lines):
        draw_text(canvas, (1678, 270 + index * 30), line, 16)

    buttons = [
        ("刷新商店 · 1", (87, 124, 116, 255)),
        ("冻结商店", (75, 110, 143, 255)),
        ("升级酒馆 · 5", (139, 105, 69, 255)),
        ("出售随从", (134, 83, 65, 255)),
        ("结束回合", (66, 104, 76, 255)),
    ]
    for index, (label, color) in enumerate(buttons):
        top = 650 + index * 72
        rounded_panel(
            canvas,
            (1675, top, 1870, top + 52),
            fill=color,
            radius=10,
            outline=(255, 247, 226, 170),
            width=1,
        )
        draw_text(
            canvas,
            (1772, top + 27),
            label,
            16,
            (255, 250, 236, 255),
            anchor="mm",
        )
    return canvas.convert("RGB")


def render_battle(
    cards: list[dict],
    crop_mode: str,
    bright_shield: bool = False,
) -> Image.Image:
    cards_by_id = card_map(cards)
    canvas = cover(Image.open(BATTLE_BACKGROUND), SCREEN_SIZE)
    standees = StandeeRenderer(bright_shield)

    rounded_panel(canvas, (20, 18, 1900, 96), fill=(245, 235, 213, 236))
    draw_text(canvas, (48, 40), "战斗", 28, BLUE_INK)
    draw_text(canvas, (155, 45), "第 7 回合", 20)
    draw_text(canvas, (330, 45), "行动中 · 速度 1×", 20, MUTED)
    draw_text(
        canvas,
        (1870, 45),
        "跳过表现   查看结算",
        18,
        MUTED,
        anchor="ra",
    )

    rounded_panel(canvas, (1540, 112, 1900, 962), fill=(246, 236, 213, 232))
    draw_text(canvas, (1566, 132), "战斗日志", 22, BLUE_INK)
    ImageDraw.Draw(canvas).line(
        (1566, 172, 1874, 172),
        fill=(159, 130, 91, 170),
        width=1,
    )

    log_lines = [
        "01  铸魂盾侍获得护盾",
        "02  幼鹿灵攻击微光术士",
        "03  共鸣钟卫失去护盾",
        "04  归根引魂者触发亡语",
        "05  命轨记录员获得 +1/+1",
        "06  镜钢决斗家获得溅射",
        "07  群山古灵永久成长",
    ]
    for index, line in enumerate(log_lines):
        draw_text(canvas, (1566, 202 + index * 42), line, 15, INK)
    draw_text(canvas, (1566, 525), "当前裁切", 15, MUTED)
    crop_label = (
        "v0.3.3：居中裁切 + 明亮护盾"
        if bright_shield
        else (
            "Runtime：强制拉伸"
            if crop_mode == "runtime-stretch"
            else "建议：居中裁切"
        )
    )
    draw_text(
        canvas,
        (1566, 558),
        crop_label,
        19,
        GREEN if bright_shield or crop_mode != "runtime-stretch" else RED,
    )
    draw_text(canvas, (1566, 610), "重点检查", 15, MUTED)
    checks = [
        "主体比例是否自然",
        "金色框是否可识别",
        "护盾/亡语标记是否清楚",
        "攻击与生命是否可读",
    ]
    for index, line in enumerate(checks):
        draw_text(canvas, (1566, 644 + index * 34), "· " + line, 15)

    rounded_panel(
        canvas,
        (42, 122, 170, 168),
        fill=(158, 73, 59, 224),
        radius=12,
        outline=(255, 235, 211, 150),
        width=1,
    )
    draw_text(
        canvas,
        (106, 145),
        "敌方 5/5",
        16,
        (255, 244, 229, 255),
        anchor="mm",
    )
    rounded_panel(
        canvas,
        (42, 575, 170, 621),
        fill=(59, 103, 116, 224),
        radius=12,
        outline=(230, 247, 242, 150),
        width=1,
    )
    draw_text(
        canvas,
        (106, 598),
        "玩家 5/5",
        16,
        (245, 251, 240, 255),
        anchor="mm",
    )

    enemy_ids = [
        "forge_soul_shield_squire",
        "young_deer_spirit",
        "glimmer_mage",
        "old_tower_guide",
        "mirrorsteel_duelist",
    ]
    enemy_golden = {"glimmer_mage"}
    player_ids = [
        "resonance_bell_guard",
        "rootbound_soul_guide",
        "fate_track_recorder",
        "moonwheel_dispatcher",
        "ancient_mountain_spirit",
    ]
    player_golden = {"resonance_bell_guard", "ancient_mountain_spirit"}
    row_width = 5 * 160 + 4 * 62
    row_x = 210
    for index, card_id in enumerate(enemy_ids):
        standee = standees.render(
            cards_by_id[card_id],
            card_id in enemy_golden,
            crop_mode,
        )
        paste_with_shadow(canvas, standee, (row_x + index * 222, 175), 9, (0, 7))
    for index, card_id in enumerate(player_ids):
        standee = standees.render(
            cards_by_id[card_id],
            card_id in player_golden,
            crop_mode,
        )
        paste_with_shadow(canvas, standee, (row_x + index * 222, 635), 9, (0, 7))

    rounded_panel(
        canvas,
        (575, 493, 1175, 555),
        fill=(247, 238, 218, 218),
        radius=18,
    )
    draw_text(
        canvas,
        (875, 524),
        "敌方行动 · 幼鹿灵 → 共鸣钟卫",
        19,
        INK,
        anchor="mm",
    )
    return canvas.convert("RGB")


def main() -> None:
    cards, focal_points = load_cards()
    if len(cards) != 15:
        raise ValueError(f"Expected 15 cards, got {len(cards)}")

    SCENE_ROOT.mkdir(parents=True, exist_ok=True)
    CARD_OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    renderer = CARD_RENDERER.CardRenderer()
    export_card_assets(renderer, cards, focal_points)

    outputs = {
        "shop-pressure-1920x1080.png": render_shop(
            renderer,
            cards,
            focal_points,
        ),
        "battle-pressure-runtime-stretch-1920x1080.png": render_battle(
            cards,
            "runtime-stretch",
        ),
        "battle-pressure-center-crop-1920x1080.png": render_battle(
            cards,
            "center-crop",
        ),
        "battle-pressure-v0.3.3-candidate-1920x1080.png": render_battle(
            cards,
            "center-crop",
            bright_shield=True,
        ),
    }
    for name, image in outputs.items():
        path = SCENE_ROOT / name
        image.save(path, optimize=True)
        print(path)


if __name__ == "__main__":
    main()
