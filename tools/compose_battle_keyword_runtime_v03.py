from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "ui-concepts/phase-9b/battle-keyword-validation-v0.3"
RUNTIME = OUT / "runtime-assets"
RENDERS = OUT / "renders"
ASSETS = ROOT / "ui-concepts/phase-9b/battle-keyword-assets-v0.1"
BG_PATH = ROOT / "ui-concepts/unity-validation/pf-battle-screen-v0.1/battle-screen-1920x1080.png"
CARD_PATH = ROOT / "ui-concepts/phase-9b/size-validation-v0.1/renders/shield-compact.png"
HOOF_PATH = ROOT / "ui-concepts/phase-9b/size-validation-v0.1/renders/hoof-compact.png"
FONT_PATH = ROOT / "sc/Assets/Art/Fonts/NotoSansCJKsc-Regular.otf"


def trim_alpha(image: Image.Image, threshold: int = 8) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A").point(lambda value: 255 if value > threshold else 0)
    bbox = alpha.getbbox()
    return rgba.crop(bbox) if bbox else rgba


def trim_black(image: Image.Image, threshold: int = 10) -> Image.Image:
    rgb = image.convert("RGB")
    mask = rgb.convert("L").point(lambda value: 255 if value > threshold else 0)
    bbox = mask.getbbox()
    return rgb.crop(bbox) if bbox else rgb


def fit(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    copy = image.copy()
    copy.thumbnail(size, Image.Resampling.LANCZOS)
    return copy


def simplify_bookmark(source: Path, size: tuple[int, int]) -> Image.Image:
    image = trim_alpha(Image.open(source))
    image = fit(image, size)
    image = ImageEnhance.Contrast(image).enhance(1.10)
    image = ImageEnhance.Sharpness(image).enhance(1.25)
    return image


def screen(base: Image.Image, effect: Image.Image, xy: tuple[int, int]) -> None:
    effect = effect.convert("RGB")
    x, y = xy
    left = max(0, x)
    top = max(0, y)
    right = min(base.width, x + effect.width)
    bottom = min(base.height, y + effect.height)
    if right <= left or bottom <= top:
        return
    source = effect.crop((left - x, top - y, right - x, bottom - y))
    target = base.crop((left, top, right, bottom)).convert("RGB")
    screened = Image.new("RGB", target.size)
    screened.putdata(
        [
            tuple(255 - ((255 - a) * (255 - b) // 255) for a, b in zip(bg, fx))
            for bg, fx in zip(target.getdata(), source.getdata())
        ]
    )
    base.paste(screened, (left, top))


def load_effect(path: Path, box: tuple[int, int], brightness: float = 1.0) -> Image.Image:
    effect = fit(trim_black(Image.open(path)), box)
    return ImageEnhance.Brightness(effect).enhance(brightness)


def label(draw: ImageDraw.ImageDraw, text: str, xy: tuple[int, int], size: int = 20) -> None:
    font = ImageFont.truetype(str(FONT_PATH), size)
    x, y = xy
    box = draw.textbbox((x, y), text, font=font)
    draw.rounded_rectangle(
        (box[0] - 8, box[1] - 4, box[2] + 8, box[3] + 5),
        radius=6,
        fill=(20, 25, 29, 215),
        outline=(216, 185, 126, 210),
        width=1,
    )
    draw.text((x, y), text, font=font, fill=(255, 239, 204, 255))


def prepare_runtime_assets() -> dict[str, Image.Image]:
    RUNTIME.mkdir(parents=True, exist_ok=True)
    sources = {
        "taunt": ASSETS / "bookmarks/bookmark-taunt.png",
        "death": ASSETS / "bookmarks/bookmark-deathrattle.png",
        "splash": ASSETS / "bookmarks/bookmark-splash.png",
        "overflow": ASSETS / "bookmarks/bookmark-overflow-blank.png",
    }
    sizes = {"taunt": (31, 48), "death": (31, 48), "splash": (31, 48), "overflow": (34, 42)}
    results = {}
    for name, source in sources.items():
        asset = simplify_bookmark(source, sizes[name])
        asset.save(RUNTIME / f"bookmark-{name}-runtime.png")
        results[name] = asset
    return results


def put_card(
    scene: Image.Image,
    card: Image.Image,
    bookmarks: dict[str, Image.Image],
    xy: tuple[int, int],
    keys: list[str],
    shield: Image.Image | None = None,
) -> None:
    x, y = xy
    scene.alpha_composite(card, (x, y))
    if shield is not None:
        screen(scene, shield, (x - 10, y - 10))
    for index, key in enumerate(keys[:3]):
        tab = bookmarks[key]
        scene.alpha_composite(tab, (x + 151, y + 10 + index * 45))


def base_scene() -> Image.Image:
    scene = Image.open(BG_PATH).convert("RGBA")
    shade = Image.new("RGBA", scene.size, (4, 18, 17, 54))
    scene.alpha_composite(shade)
    return scene


def render_keyword_scene(bookmarks: dict[str, Image.Image], card: Image.Image) -> None:
    scene = base_scene()
    draw = ImageDraw.Draw(scene, "RGBA")
    draw.rounded_rectangle((275, 545, 1305, 890), radius=10, fill=(12, 18, 24, 185))
    shield = load_effect(ASSETS / "shield/frames/02-intact.png", (174, 250), 0.38)
    positions = [(300, 590), (500, 590), (700, 590), (900, 590), (1100, 590)]
    specs = [
        (["taunt"], None, "单关键词"),
        (["taunt", "death"], None, "双关键词"),
        (["taunt", "death", "splash"], None, "三关键词"),
        (["taunt", "death", "splash"], shield, "三关键词＋护盾"),
        (["taunt", "death", "overflow"], None, "四关键词溢出"),
    ]
    for position, (keys, shield_effect, text) in zip(positions, specs):
        put_card(scene, card, bookmarks, position, keys, shield_effect)
        label(draw, text, (position[0] + 16, 845), 17)
    label(draw, "运行时精简书签 · 真实 160×240", (300, 548), 18)
    scene.convert("RGB").save(RENDERS / "battle-keywords-runtime-1920x1080.png", quality=95)


def render_deathrattle_scene(bookmarks: dict[str, Image.Image], card: Image.Image) -> None:
    scene = base_scene()
    draw = ImageDraw.Draw(scene, "RGBA")
    x, y = 710, 590
    draw.rounded_rectangle((660, 535, 975, 880), radius=10, fill=(12, 18, 24, 160))
    put_card(scene, card, bookmarks, (x, y), ["death"])
    effect = load_effect(
        ASSETS / "events/deathrattle/frames/04-split-open.png",
        (142, 142),
        0.82,
    )
    screen(scene, effect, (x + 9, y + 13))
    label(draw, "亡语触发：蜡封崩裂", (680, 548), 19)
    scene.convert("RGB").save(RENDERS / "battle-deathrattle-runtime-1920x1080.png", quality=95)


def render_splash_scene(bookmarks: dict[str, Image.Image], card: Image.Image) -> None:
    scene = base_scene()
    draw = ImageDraw.Draw(scene, "RGBA")
    targets = [(500, 590), (710, 590), (920, 590)]
    for index, position in enumerate(targets):
        put_card(scene, card, bookmarks, position, ["splash"] if index == 1 else [])
    effect = load_effect(
        ASSETS / "events/splash/frames/05-branch-targets.png",
        (405, 225),
        0.72,
    )
    screen(scene, effect, (552, 588))
    label(draw, "溅射触发：主命中分向两个次目标", (610, 548), 19)
    scene.convert("RGB").save(RENDERS / "battle-splash-runtime-1920x1080.png", quality=95)


def main() -> None:
    RENDERS.mkdir(parents=True, exist_ok=True)
    bookmarks = prepare_runtime_assets()
    card = Image.open(CARD_PATH).convert("RGBA")
    hoof = Image.open(HOOF_PATH).convert("RGBA")
    render_keyword_scene(bookmarks, card)
    render_deathrattle_scene(bookmarks, hoof)
    render_splash_scene(bookmarks, hoof)


if __name__ == "__main__":
    main()
