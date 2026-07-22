"""Transfer a gold material reference onto the locked Phase 9B card-frame geometry."""

from __future__ import annotations

import argparse
from pathlib import Path

import cv2
import numpy as np
from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("normal", type=Path)
    parser.add_argument("reference", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--validation-out", type=Path)
    return parser.parse_args()


def luminance(rgb: np.ndarray) -> np.ndarray:
    return (
        0.2126 * rgb[:, :, 0]
        + 0.7152 * rgb[:, :, 1]
        + 0.0722 * rgb[:, :, 2]
    )


def histogram_mapping(
    source_luma: np.ndarray,
    source_mask: np.ndarray,
    reference_luma: np.ndarray,
    reference_mask: np.ndarray,
) -> np.ndarray:
    source_values = np.clip(source_luma[source_mask], 0, 255).astype(np.uint8)
    reference_values = np.clip(reference_luma[reference_mask], 0, 255).astype(np.uint8)
    source_cdf = np.cumsum(np.bincount(source_values, minlength=256)).astype(float)
    reference_cdf = np.cumsum(np.bincount(reference_values, minlength=256)).astype(float)
    source_cdf /= source_cdf[-1]
    reference_cdf /= reference_cdf[-1]
    return np.interp(source_cdf, reference_cdf, np.arange(256, dtype=float))


def reference_palette(
    reference_rgb: np.ndarray,
    reference_luma: np.ndarray,
    reference_mask: np.ndarray,
) -> tuple[np.ndarray, np.ndarray]:
    levels = np.percentile(reference_luma[reference_mask], np.linspace(0, 100, 17))
    palette_luma: list[float] = []
    palette_rgb: list[np.ndarray] = []
    for low, high in zip(levels[:-1], levels[1:]):
        band = reference_mask & (reference_luma >= low) & (reference_luma <= high)
        palette_luma.append(float(np.median(reference_luma[band])))
        palette_rgb.append(np.median(reference_rgb[band], axis=0))

    x = np.asarray(palette_luma)
    colors = np.asarray(palette_rgb)
    unique_x, unique_indices = np.unique(x, return_index=True)
    return unique_x, colors[unique_indices]


def goldenize(normal: Image.Image, reference: Image.Image) -> Image.Image:
    if normal.size != reference.size:
        raise ValueError("Normal frame and gold reference must have identical dimensions.")

    normal_rgba = np.asarray(normal.convert("RGBA"))
    normal_rgb = normal_rgba[:, :, :3]
    alpha = normal_rgba[:, :, 3]
    reference_rgb = np.asarray(reference.convert("RGB"))

    normal_luma = luminance(normal_rgb)
    reference_luma = luminance(reference_rgb)
    normal_mask = alpha >= 128
    reference_mask = (
        (reference_luma > 12)
        & (reference_rgb[:, :, 0] > reference_rgb[:, :, 1] * 1.04)
        & (reference_rgb[:, :, 0] > reference_rgb[:, :, 2] * 1.25)
    )

    luma_map = histogram_mapping(
        normal_luma,
        normal_mask,
        reference_luma,
        reference_mask,
    )
    mapped_luma = luma_map[np.clip(normal_luma, 0, 255).astype(np.uint8)]
    palette_luma, palette_rgb = reference_palette(
        reference_rgb,
        reference_luma,
        reference_mask,
    )
    gold_rgb = np.stack(
        [np.interp(mapped_luma, palette_luma, palette_rgb[:, channel]) for channel in range(3)],
        axis=2,
    )

    reference_gray = np.clip(reference_luma, 0, 255).astype(np.uint8)
    reference_foreground = (reference_luma > 8).astype(np.uint8)
    reference_surface = cv2.morphologyEx(
        reference_foreground,
        cv2.MORPH_CLOSE,
        cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9)),
    )
    reference_surface = cv2.erode(reference_surface, np.ones((5, 5), np.uint8)) != 0
    target_distance = cv2.distanceTransform(normal_mask.astype(np.uint8), cv2.DIST_L2, 5)
    texture_overlap = reference_surface & (target_distance > 2.0)

    black_hat = cv2.morphologyEx(
        reference_gray,
        cv2.MORPH_BLACKHAT,
        cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9)),
    ).astype(float)
    cracks = np.clip((black_hat - 38.0) / 125.0, 0.0, 1.0) ** 1.35
    cracks *= texture_overlap
    gold_rgb *= (1.0 - 0.28 * cracks[:, :, None])

    top_hat = cv2.morphologyEx(
        reference_gray,
        cv2.MORPH_TOPHAT,
        cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7)),
    ).astype(float)
    highlights = np.clip((top_hat - 22.0) / 110.0, 0.0, 1.0) * texture_overlap
    gold_rgb += highlights[:, :, None] * np.array([24.0, 18.0, 6.0])

    process_mask = (alpha > 0) | np.any(normal_rgb != 0, axis=2)
    gold_rgb[~process_mask] = 0
    return Image.fromarray(
        np.dstack((np.clip(gold_rgb, 0, 255).astype(np.uint8), alpha)),
        "RGBA",
    )


def make_validation(frame: Image.Image) -> Image.Image:
    thumb = frame.resize((512, 768), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", (1024, 1536))
    backgrounds = ((255, 255, 255), (18, 18, 20), (95, 24, 44), (15, 92, 96))
    for index, color in enumerate(backgrounds):
        background = Image.new("RGBA", thumb.size, color + (255,))
        background.alpha_composite(thumb)
        canvas.paste(background.convert("RGB"), ((index % 2) * 512, (index // 2) * 768))
    return canvas


def main() -> None:
    args = parse_args()
    with Image.open(args.normal) as normal, Image.open(args.reference) as reference:
        golden = goldenize(normal, reference)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    golden.save(args.output, optimize=True)
    if args.validation_out:
        args.validation_out.parent.mkdir(parents=True, exist_ok=True)
        make_validation(golden).save(args.validation_out, optimize=True)


if __name__ == "__main__":
    main()
