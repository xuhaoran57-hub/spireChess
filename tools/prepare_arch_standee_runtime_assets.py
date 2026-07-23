from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5"


def trim_alpha(path: Path) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    bbox = image.getchannel("A").getbbox()
    return image.crop(bbox) if bbox else image


def trim_black(path: Path) -> Image.Image:
    image = Image.open(path).convert("RGB")
    mask = image.convert("L").point(lambda value: 255 if value > 8 else 0)
    bbox = mask.getbbox()
    return image.crop(bbox) if bbox else image


def fit_and_save(image: Image.Image, size: tuple[int, int], destination: Path) -> None:
    image.thumbnail(size, Image.Resampling.LANCZOS)
    image.save(destination)


def main() -> None:
    output = BASE / "runtime"
    output.mkdir(parents=True, exist_ok=True)
    alpha = BASE / "alpha"
    specs = {
        "standee-frame.png": (296, 376),
        "attack-medallion.png": (96, 96),
        "health-medallion.png": (96, 96),
        "deathrattle-seal.png": (64, 64),
        "splash-mark.png": (44, 44),
        "taunt-base.png": (176, 48),
    }
    for name, size in specs.items():
        fit_and_save(trim_alpha(alpha / name), size, output / name)
    shield = trim_black(output / "shield-shell-screen.png")
    fit_and_save(shield, (336, 428), output / "shield-shell-screen-2x.png")


if __name__ == "__main__":
    main()
