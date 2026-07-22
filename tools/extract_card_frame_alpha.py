"""Extract a real alpha channel from the Phase 9B checkerboard card frame."""

from __future__ import annotations

import argparse
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageFilter


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("input", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--validation-out", type=Path)
    parser.add_argument("--threshold", type=int, default=235)
    return parser.parse_args()


def extract_frame(source: Image.Image, threshold: int) -> Image.Image:
    rgb = np.asarray(source.convert("RGB"))
    gray = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)

    candidate = (gray < threshold).astype(np.uint8)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(candidate, 8)
    if count < 2:
        raise ValueError("No foreground component found; check the threshold.")

    largest_label = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    frame_mask = labels == largest_label

    # Thresholding can punch tiny transparent specks through bright metal
    # highlights. Preserve only the outer background and the five intentional
    # card openings; fill every smaller enclosed hole back into the frame.
    background_count, background_labels, background_stats, _ = (
        cv2.connectedComponentsWithStats((~frame_mask).astype(np.uint8), 8)
    )
    height, width = frame_mask.shape
    for label in range(1, background_count):
        x, y, component_width, component_height, area = background_stats[label]
        touches_edge = (
            x == 0
            or y == 0
            or x + component_width == width
            or y + component_height == height
        )
        if not touches_edge and area < 10_000:
            frame_mask[background_labels == label] = True

    matte = Image.fromarray((frame_mask * 255).astype(np.uint8))
    alpha = np.asarray(matte.filter(ImageFilter.GaussianBlur(0.65))).copy()
    alpha[alpha <= 1] = 0
    alpha[alpha >= 254] = 255

    # Replace checker-contaminated edge RGB with the nearest interior frame
    # color. Keep a four-pixel color bleed under transparent pixels so Unity's
    # bilinear filtering cannot pull white checker colors into the silhouette.
    kernel = np.ones((3, 3), np.uint8)
    interior = cv2.erode(frame_mask.astype(np.uint8), kernel, iterations=1) != 0
    search = (~interior).astype(np.uint8)
    distance, nearest_labels = cv2.distanceTransformWithLabels(
        search,
        cv2.DIST_L2,
        5,
        labelType=cv2.DIST_LABEL_PIXEL,
    )
    color_lut = np.zeros((int(nearest_labels.max()) + 1, 3), dtype=np.uint8)
    color_lut[nearest_labels[interior]] = rgb[interior]
    nearest_rgb = color_lut[nearest_labels]

    clean_rgb = rgb.copy()
    edge_or_bleed = (alpha < 254) & (distance <= 4.0)
    clean_rgb[edge_or_bleed] = nearest_rgb[edge_or_bleed]
    clean_rgb[(alpha == 0) & (distance > 4.0)] = 0

    return Image.fromarray(np.dstack((clean_rgb, alpha)), "RGBA")


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
    with Image.open(args.input) as source:
        frame = extract_frame(source, args.threshold)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    frame.save(args.output, optimize=True)

    if args.validation_out:
        args.validation_out.parent.mkdir(parents=True, exist_ok=True)
        make_validation(frame).save(args.validation_out, optimize=True)


if __name__ == "__main__":
    main()
