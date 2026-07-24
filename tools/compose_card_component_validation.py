from pathlib import Path
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = (
    Path(__file__).resolve().parents[1]
    / "ui-concepts"
    / "phase-9b"
    / "card-components-number-medallions-v0.1"
)
FONT_PATH = (
    Path(__file__).resolve().parents[1]
    / "sc"
    / "Assets"
    / "Art"
    / "Fonts"
    / "NotoSansCJKsc-Regular.otf"
)


def draw_centered(draw, text, center_x, y, font, fill):
    bounds = draw.textbbox((0, 0), text, font=font)
    width = bounds[2] - bounds[0]
    draw.text((center_x - width / 2, y), text, font=font, fill=fill)


def main():
    source = Image.open(
        ROOT / "component-strip-card-validation-v0.1.png"
    ).convert("RGB")
    canvas = source.copy()
    draw = ImageDraw.Draw(canvas)

    random.seed(9)
    draw.rectangle((10, 389, 1525, 1013), fill=(234, 216, 175))
    paper_colors = (
        (226, 204, 159),
        (241, 226, 193),
        (219, 195, 151),
    )
    for _ in range(26000):
        draw.point(
            (random.randint(12, 1523), random.randint(391, 1011)),
            fill=random.choice(paper_colors),
        )
    draw.line((18, 389, 1518, 389), fill=(166, 120, 70), width=1)

    full = Image.open(ROOT / "full-240x360-v0.1.png").convert("RGB")
    compact = Image.open(ROOT / "compact-160x240-v0.1.png").convert("RGB")
    full = full.resize((360, 540), Image.Resampling.LANCZOS)
    compact = compact.resize((240, 360), Image.Resampling.LANCZOS)

    shadow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    shadow_draw.rounded_rectangle(
        (304, 454, 672, 1002),
        radius=10,
        fill=(64, 38, 20, 90),
    )
    shadow_draw.rounded_rectangle(
        (1044, 574, 1292, 942),
        radius=8,
        fill=(64, 38, 20, 90),
    )
    shadow = shadow.filter(ImageFilter.GaussianBlur(8))
    canvas = Image.alpha_composite(canvas.convert("RGBA"), shadow).convert("RGB")
    canvas.paste(full, (300, 450))
    canvas.paste(compact, (1040, 570))

    draw = ImageDraw.Draw(canvas)
    title_font = ImageFont.truetype(str(FONT_PATH), 32)
    measure_font = ImageFont.truetype(str(FONT_PATH), 18)
    ink = (69, 48, 31)
    line = (130, 91, 55)

    draw_centered(draw, "FULL 240×360", 480, 402, title_font, ink)
    draw_centered(draw, "COMPACT 160×240", 1160, 522, title_font, ink)

    draw.line((270, 450, 270, 990), fill=line, width=2)
    draw.line((260, 450, 280, 450), fill=line, width=2)
    draw.line((260, 990, 280, 990), fill=line, width=2)
    draw.text((190, 704), "360 px", font=measure_font, fill=ink)
    draw.line((300, 1000, 660, 1000), fill=line, width=2)
    draw.line((300, 990, 300, 1010), fill=line, width=2)
    draw.line((660, 990, 660, 1010), fill=line, width=2)
    draw_centered(draw, "240 px", 480, 973, measure_font, ink)

    draw.line((1010, 570, 1010, 930), fill=line, width=2)
    draw.line((1000, 570, 1020, 570), fill=line, width=2)
    draw.line((1000, 930, 1020, 930), fill=line, width=2)
    draw.text((936, 734), "240 px", font=measure_font, fill=ink)
    draw.line((1040, 946, 1280, 946), fill=line, width=2)
    draw.line((1040, 936, 1040, 956), fill=line, width=2)
    draw.line((1280, 936, 1280, 956), fill=line, width=2)
    draw_centered(draw, "160 px", 1160, 919, measure_font, ink)

    canvas.save(
        ROOT / "component-strip-card-validation-measured-v0.1.png",
        "PNG",
    )


if __name__ == "__main__":
    main()
