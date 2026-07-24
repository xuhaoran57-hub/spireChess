"""Build review sheets for the Phase 9B archetype anchor illustrations."""

from __future__ import annotations

import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parent.parent
OUT = (
    ROOT
    / "ui-concepts"
    / "phase-9b"
    / "archetype-anchor-illustrations-v0.2"
)
MASTERS = OUT / "masters"
REVIEW = OUT / "review"
THUMBNAILS = REVIEW / "thumbnails-160x240"
FONT_PATH = ROOT / "sc" / "Assets" / "Art" / "Fonts" / "NotoSansCJKsc-Regular.otf"

GROUPS = (
    {
        "category": "铸魂  FORGE SOUL",
        "color": (139, 67, 39, 255),
        "cards": (
            ("铸魂盾侍", "低阶锚点 · 封闭直边块体", "forge-soul-shield-squire.png"),
            ("不熄炉王", "高阶对应 · 端坐空甲王者", "forge-undying-furnace-king.png"),
        ),
    },
    {
        "category": "荒灵  WILD SPIRIT",
        "color": (91, 112, 60, 255),
        "cards": (
            ("幼鹿灵", "低阶锚点 · 分叉有机破边", "wild-young-deer-spirit.png"),
            ("万蹄奔潮", "高阶对应 · 冲锋枝冠体量", "wild-ten-thousand-hoof-surge.png"),
        ),
    },
    {
        "category": "星契  STAR PACT",
        "color": (55, 72, 124, 255),
        "cards": (
            ("星盘校准师", "低阶锚点 · 轻薄开放圆弧", "star-astrolabe-calibrator.png"),
            ("天穹契约者", "高阶对应 · 纵向织带轨道", "star-sky-covenant-bearer.png"),
        ),
    },
    {
        "category": "旅团  BRIGADE",
        "color": (133, 99, 63, 255),
        "cards": (
            ("行脚医师", "低阶锚点 · 实用非对称负重", "wayfarer-traveling-physician.png"),
            ("百技学徒", "第二锚点 · 拼装训练装备", "wayfarer-many-arts-apprentice.png"),
        ),
    },
)


def font(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONT_PATH), size)


def centered_text(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    text: str,
    text_font: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int, int],
) -> None:
    bounds = draw.textbbox((0, 0), text, font=text_font)
    width = bounds[2] - bounds[0]
    height = bounds[3] - bounds[1]
    x = box[0] + (box[2] - box[0] - width) / 2 - bounds[0]
    y = box[1] + (box[3] - box[1] - height) / 2 - bounds[1]
    draw.text((x, y), text, font=text_font, fill=fill)


def paper_canvas(size: tuple[int, int], seed: int) -> Image.Image:
    image = Image.new("RGBA", size, (238, 223, 190, 255))
    draw = ImageDraw.Draw(image, "RGBA")
    rng = random.Random(seed)
    for _ in range(round(size[0] * size[1] / 150)):
        draw.point(
            (rng.randrange(size[0]), rng.randrange(size[1])),
            fill=rng.choice(
                (
                    (111, 75, 39, 12),
                    (255, 251, 226, 18),
                    (149, 105, 59, 8),
                )
            ),
        )
    return image


def load_master(filename: str) -> Image.Image:
    image = Image.open(MASTERS / filename).convert("RGB")
    if image.size != (1024, 1536):
        raise ValueError(f"{filename} has unexpected size {image.size}")
    return image


def add_art(
    canvas: Image.Image,
    filename: str,
    box: tuple[int, int, int, int],
) -> None:
    art = load_master(filename).resize(
        (box[2] - box[0], box[3] - box[1]),
        Image.Resampling.LANCZOS,
    )
    canvas.paste(art, (box[0], box[1]))
    ImageDraw.Draw(canvas, "RGBA").rectangle(
        box,
        outline=(84, 59, 38, 132),
        width=2,
    )


def build_main_review() -> Image.Image:
    canvas = paper_canvas((2048, 1536), seed=241)
    draw = ImageDraw.Draw(canvas, "RGBA")
    ink = (67, 47, 31, 255)
    muted = (119, 84, 52, 230)

    centered_text(
        draw,
        (70, 24, 1978, 80),
        "四类锚点立绘 v0.2 · 统一方向评审",
        font(42),
        ink,
    )
    centered_text(
        draw,
        (70, 76, 1978, 110),
        "每类两张｜同一媒介与一级几何｜角色造型不复用｜原图 1024×1536",
        font(19),
        muted,
    )

    column_width = 480
    start_x = 64
    image_width = 300
    image_height = 450
    for index, group in enumerate(GROUPS):
        left = start_x + index * column_width
        right = left + 448
        draw.rounded_rectangle(
            (left, 122, right, 1316),
            radius=20,
            fill=(248, 235, 204, 104),
            outline=(126, 89, 53, 60),
            width=1,
        )
        draw.rounded_rectangle(
            (left + 24, 138, right - 24, 181),
            radius=12,
            fill=group["color"],
        )
        centered_text(
            draw,
            (left + 30, 139, right - 30, 180),
            group["category"],
            font(22),
            (249, 239, 213, 255),
        )

        for row, (name, role, filename) in enumerate(group["cards"]):
            art_left = left + (448 - image_width) // 2
            art_top = 200 + row * 548
            art_box = (
                art_left,
                art_top,
                art_left + image_width,
                art_top + image_height,
            )
            add_art(canvas, filename, art_box)
            centered_text(
                draw,
                (left + 20, art_top + 458, right - 20, art_top + 493),
                name,
                font(23),
                ink,
            )
            centered_text(
                draw,
                (left + 20, art_top + 491, right - 20, art_top + 520),
                role,
                font(15),
                muted,
            )

    draw.line((64, 1340, 1984, 1340), fill=(133, 93, 55, 100), width=1)
    centered_text(
        draw,
        (100, 1350, 1948, 1384),
        "160×240 缩略检查",
        font(20),
        ink,
    )
    thumbnail_width = 80
    thumbnail_height = 120
    gap = 28
    filenames = [
        filename
        for group in GROUPS
        for _, _, filename in group["cards"]
    ]
    total_width = len(filenames) * thumbnail_width + (len(filenames) - 1) * gap
    x = (2048 - total_width) // 2
    for filename in filenames:
        thumb = load_master(filename).resize(
            (thumbnail_width, thumbnail_height),
            Image.Resampling.LANCZOS,
        )
        canvas.paste(thumb, (x, 1394))
        x += thumbnail_width + gap
    return canvas


def export_exact_thumbnails() -> None:
    THUMBNAILS.mkdir(parents=True, exist_ok=True)
    for group in GROUPS:
        for _, _, filename in group["cards"]:
            load_master(filename).resize(
                (160, 240),
                Image.Resampling.LANCZOS,
            ).save(THUMBNAILS / filename, optimize=True)


def build_thumbnail_review() -> Image.Image:
    canvas = paper_canvas((1536, 720), seed=242)
    draw = ImageDraw.Draw(canvas, "RGBA")
    ink = (67, 47, 31, 255)
    muted = (119, 84, 52, 230)
    centered_text(
        draw,
        (60, 20, 1476, 64),
        "160×240 原尺寸可读性检查",
        font(30),
        ink,
    )
    centered_text(
        draw,
        (60, 62, 1476, 90),
        "不依赖文字、背景或边框判断主体一级形状",
        font(17),
        muted,
    )

    centers = (220, 585, 950, 1315)
    for column, group in enumerate(GROUPS):
        centered_text(
            draw,
            (centers[column] - 155, 94, centers[column] + 155, 124),
            group["category"],
            font(18),
            group["color"],
        )
        for row, (name, _, filename) in enumerate(group["cards"]):
            top = 135 + row * 285
            thumb = Image.open(THUMBNAILS / filename).convert("RGB")
            left = centers[column] - 80
            canvas.paste(thumb, (left, top))
            draw.rectangle(
                (left, top, left + 160, top + 240),
                outline=(84, 59, 38, 132),
                width=1,
            )
            centered_text(
                draw,
                (centers[column] - 130, top + 243, centers[column] + 130, top + 270),
                name,
                font(16),
                ink,
            )
    return canvas


def main() -> None:
    REVIEW.mkdir(parents=True, exist_ok=True)
    export_exact_thumbnails()
    build_main_review().save(
        REVIEW / "eight-illustrations-review-v0.2.png",
        optimize=True,
    )
    build_thumbnail_review().save(
        REVIEW / "thumbnail-review-160x240-v0.2.png",
        optimize=True,
    )


if __name__ == "__main__":
    main()
