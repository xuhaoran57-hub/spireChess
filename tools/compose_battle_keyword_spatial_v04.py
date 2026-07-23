from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "ui-concepts/phase-9b/battle-keyword-validation-v0.4"
RENDERS = OUT / "renders"
BG = ROOT / "ui-concepts/unity-validation/pf-battle-screen-v0.1/battle-screen-1920x1080.png"
CARD = ROOT / "ui-concepts/phase-9b/size-validation-v0.1/renders/shield-compact.png"
FONT = ROOT / "sc/Assets/Art/Fonts/NotoSansCJKsc-Regular.otf"


def font(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT), size)


def line(draw: ImageDraw.ImageDraw, points, fill, width: int) -> None:
    draw.line(points, fill=fill, width=width, joint="curve")


def deathrattle_behind(scene: Image.Image, x: int, y: int) -> None:
    layer = Image.new("RGBA", scene.size)
    draw = ImageDraw.Draw(layer)
    purple = (116, 70, 132, 105)
    glow = (197, 139, 214, 50)
    draw.ellipse((x - 12, y + 150, x + 172, y + 278), outline=glow, width=15)
    draw.ellipse((x - 4, y + 158, x + 164, y + 270), outline=purple, width=7)
    for px, py, radius in [(-2, 218, 8), (162, 218, 8), (32, 252, 7), (128, 252, 6)]:
        draw.ellipse((x + px - radius, y + py - radius, x + px + radius, y + py + radius), fill=purple)
    scene.alpha_composite(layer)


def taunt_outer(scene: Image.Image, x: int, y: int) -> None:
    layer = Image.new("RGBA", scene.size)
    draw = ImageDraw.Draw(layer)
    clay = (171, 68, 43, 190)
    cream = (237, 201, 139, 150)
    line(draw, [(x - 12, y + 54), (x - 18, y + 32), (x - 18, y + 212), (x - 8, y + 232)], clay, 9)
    line(draw, [(x + 172, y + 54), (x + 178, y + 32), (x + 178, y + 212), (x + 168, y + 232)], clay, 9)
    line(draw, [(x - 15, y + 35), (x + 20, y + 16), (x + 140, y + 16), (x + 175, y + 35)], clay, 9)
    line(draw, [(x - 13, y + 36), (x + 20, y + 22), (x + 140, y + 22), (x + 173, y + 36)], cream, 2)
    scene.alpha_composite(layer)


def shield_surface(scene: Image.Image, x: int, y: int) -> None:
    layer = Image.new("RGBA", scene.size)
    draw = ImageDraw.Draw(layer)
    cyan = (126, 207, 224, 130)
    pale = (218, 246, 247, 135)
    wash = (116, 191, 209, 22)
    polygon = [
        (x + 7, y + 10),
        (x + 27, y + 2),
        (x + 133, y + 2),
        (x + 153, y + 10),
        (x + 153, y + 198),
        (x + 139, y + 226),
        (x + 80, y + 237),
        (x + 21, y + 226),
        (x + 7, y + 198),
    ]
    draw.polygon(polygon, fill=wash)
    line(draw, polygon + [polygon[0]], cyan, 6)
    line(draw, [(x + 17, y + 16), (x + 31, y + 10), (x + 129, y + 10), (x + 143, y + 16)], pale, 2)
    scene.alpha_composite(layer)


def splash_relation(scene: Image.Image, x: int, y: int) -> None:
    layer = Image.new("RGBA", scene.size)
    draw = ImageDraw.Draw(layer)
    teal = (62, 151, 139, 150)
    gold = (222, 184, 102, 120)
    origin = (x + 80, y + 70)
    left = [(x + 78, y + 72), (x + 58, y + 57), (x + 39, y + 48)]
    right = [(x + 82, y + 72), (x + 102, y + 57), (x + 121, y + 48)]
    line(draw, [(x + 80, y + 91), origin], teal, 5)
    line(draw, left, teal, 5)
    line(draw, right, teal, 5)
    for px, py in [left[-1], right[-1]]:
        draw.ellipse((px - 6, py - 6, px + 6, py + 6), outline=gold, width=2)
    draw.ellipse((origin[0] - 5, origin[1] - 5, origin[0] + 5, origin[1] + 5), fill=gold)
    scene.alpha_composite(layer)


def caption(scene: Image.Image, text: str, x: int, y: int, color) -> None:
    draw = ImageDraw.Draw(scene, "RGBA")
    face = font(18)
    box = draw.textbbox((x, y), text, font=face)
    draw.rounded_rectangle(
        (box[0] - 9, box[1] - 5, box[2] + 9, box[3] + 6),
        radius=6,
        fill=(14, 20, 24, 220),
        outline=color,
        width=2,
    )
    draw.text((x, y), text, font=face, fill=(255, 242, 214, 255))


def put_card(scene: Image.Image, card: Image.Image, x: int, y: int, effects: tuple[str, ...]) -> None:
    if "deathrattle" in effects:
        deathrattle_behind(scene, x, y)
    if "taunt" in effects:
        taunt_outer(scene, x, y)
    scene.alpha_composite(card, (x, y))
    if "shield" in effects:
        shield_surface(scene, x, y)
    if "splash" in effects:
        splash_relation(scene, x, y)


def main() -> None:
    RENDERS.mkdir(parents=True, exist_ok=True)
    scene = Image.open(BG).convert("RGBA")
    scene.alpha_composite(Image.new("RGBA", scene.size, (3, 17, 16, 48)))
    draw = ImageDraw.Draw(scene, "RGBA")
    draw.rounded_rectangle((250, 530, 1340, 915), radius=14, fill=(10, 16, 22, 205))
    draw.text(
        (275, 548),
        "战斗关键词空间语言 v0.4 · 静默态",
        font=font(22),
        fill=(255, 239, 203, 255),
    )
    draw.text(
        (275, 580),
        "表面层 / 外框层 / 背后层 / 关系层互不争抢；名称与攻防为禁入区",
        font=font(15),
        fill=(202, 193, 169, 255),
    )

    card = Image.open(CARD).convert("RGBA")
    positions = [290, 500, 710, 920, 1130]
    specs = [
        (("shield",), "护盾 · 表面层", (126, 207, 224, 255)),
        (("taunt",), "嘲讽 · 外框层", (190, 82, 53, 255)),
        (("deathrattle",), "亡语 · 背后层", (153, 100, 172, 255)),
        (("splash",), "溅射 · 关系层", (74, 163, 150, 255)),
        (("deathrattle", "taunt", "shield", "splash"), "四关键词组合", (223, 187, 112, 255)),
    ]
    for x, (effects, title, color) in zip(positions, specs):
        put_card(scene, card, x, 625, effects)
        caption(scene, title, x + 6, 875, color)

    scene.convert("RGB").save(RENDERS / "battle-keyword-spatial-language-v0.4-1920x1080.png", quality=95)


if __name__ == "__main__":
    main()
