"""Build review sheets for the six missing Phase 9B sample minion portraits."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

from compose_archetype_anchor_review import centered_text, font, paper_canvas


ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "ui-concepts" / "phase-9b" / "sample-minion-illustrations-v0.1"
MASTERS = OUT / "masters"
REVIEW = OUT / "review"
THUMBNAILS = REVIEW / "thumbnails-160x240"

GROUPS = (
    {
        "category": "铸魂  FORGE SOUL",
        "color": (139, 67, 39, 255),
        "cards": (
            ("回火修补匠", "T2 · 修盾与回火增益", "forge-tempering-mender.png"),
            ("裂甲复仇者", "T4 · 护盾与亡语遗产", "forge-cracked-armor-avenger.png"),
        ),
    },
    {
        "category": "荒灵  WILD SPIRIT",
        "color": (91, 112, 60, 255),
        "cards": (
            ("腐叶承嗣", "T2 · 嘲讽与战斗内传承", "wild-rotleaf-heir.png"),
            ("狐群巢母", "T4 · 嵌套亡语衍生", "wild-fox-den-matriarch.png"),
        ),
    },
    {
        "category": "星契  STAR PACT",
        "color": (55, 72, 124, 255),
        "cards": (
            ("秘页折光师", "T3 · 护盾与前两次施法", "star-secret-page-refractor.png"),
            ("星图掮客", "T3 · 刷新门槛与发现", "star-star-map-broker.png"),
        ),
    },
)


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


def export_exact_thumbnails() -> None:
    THUMBNAILS.mkdir(parents=True, exist_ok=True)
    for group in GROUPS:
        for _, _, filename in group["cards"]:
            load_master(filename).resize(
                (160, 240),
                Image.Resampling.LANCZOS,
            ).save(THUMBNAILS / filename, optimize=True)


def build_main_review() -> Image.Image:
    canvas = paper_canvas((1800, 1500), seed=251)
    draw = ImageDraw.Draw(canvas, "RGBA")
    ink = (67, 47, 31, 255)
    muted = (119, 84, 52, 230)

    centered_text(
        draw,
        (60, 22, 1740, 76),
        "阶段 9B · 其余六张样板随从立绘 v0.1",
        font(38),
        ink,
    )
    centered_text(
        draw,
        (60, 72, 1740, 108),
        "三种族各两张｜原图 1024×1536｜下方同步检查 160×240",
        font(18),
        muted,
    )

    column_width = 560
    start_x = 60
    image_width = 330
    image_height = 495
    for index, group in enumerate(GROUPS):
        left = start_x + index * column_width
        right = left + 520
        draw.rounded_rectangle(
            (left, 122, right, 1288),
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
            art_left = left + (520 - image_width) // 2
            art_top = 202 + row * 540
            art_box = (
                art_left,
                art_top,
                art_left + image_width,
                art_top + image_height,
            )
            add_art(canvas, filename, art_box)
            centered_text(
                draw,
                (left + 20, art_top + 499, right - 20, art_top + 529),
                f"{name}  ·  {role}",
                font(16),
                ink,
            )

    centered_text(
        draw,
        (70, 1304, 1730, 1336),
        "160×240 原尺寸缩略检查",
        font(20),
        ink,
    )
    filenames = [
        filename
        for group in GROUPS
        for _, _, filename in group["cards"]
    ]
    thumbnail_width = 80
    thumbnail_height = 120
    gap = 48
    total_width = len(filenames) * thumbnail_width + (len(filenames) - 1) * gap
    x = (1800 - total_width) // 2
    for filename in filenames:
        thumb = load_master(filename).resize(
            (thumbnail_width, thumbnail_height),
            Image.Resampling.LANCZOS,
        )
        canvas.paste(thumb, (x, 1352))
        x += thumbnail_width + gap
    return canvas


def build_thumbnail_review() -> Image.Image:
    canvas = paper_canvas((1280, 650), seed=252)
    draw = ImageDraw.Draw(canvas, "RGBA")
    ink = (67, 47, 31, 255)
    muted = (119, 84, 52, 230)
    centered_text(
        draw,
        (50, 18, 1230, 60),
        "其余六张样板随从 · 160×240 原尺寸检查",
        font(28),
        ink,
    )
    centered_text(
        draw,
        (50, 58, 1230, 86),
        "不依赖卡框或文字，核对主体轮廓、种族形状语言与明暗层级",
        font(16),
        muted,
    )

    centers = (220, 640, 1060)
    for column, group in enumerate(GROUPS):
        centered_text(
            draw,
            (centers[column] - 170, 92, centers[column] + 170, 122),
            group["category"],
            font(18),
            group["color"],
        )
        for row, (name, _, filename) in enumerate(group["cards"]):
            left = centers[column] - 178 + row * 196
            top = 140
            thumb = Image.open(THUMBNAILS / filename).convert("RGB")
            canvas.paste(thumb, (left, top))
            draw.rectangle(
                (left, top, left + 160, top + 240),
                outline=(84, 59, 38, 132),
                width=1,
            )
            centered_text(
                draw,
                (left - 12, top + 246, left + 172, top + 274),
                name,
                font(16),
                ink,
            )

    centered_text(
        draw,
        (50, 494, 1230, 534),
        "快速结论位置：铸魂看块体与炉芯｜荒灵看动物一级形与枝叶破边｜星契看人物、纸页与开放细弧",
        font(17),
        ink,
    )
    centered_text(
        draw,
        (50, 536, 1230, 570),
        "这张图按真实 Compact 立绘占位尺寸展示，不做锐化补偿。",
        font(15),
        muted,
    )
    return canvas


def main() -> None:
    REVIEW.mkdir(parents=True, exist_ok=True)
    export_exact_thumbnails()
    build_main_review().save(
        REVIEW / "six-sample-minions-review-v0.1.png",
        optimize=True,
    )
    build_thumbnail_review().save(
        REVIEW / "thumbnail-review-160x240-v0.1.png",
        optimize=True,
    )


if __name__ == "__main__":
    main()
