#!/usr/bin/env python3
from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
RELIC_DIR = ROOT / "Diviner" / "images" / "relics"
RELIC_BIG_DIR = RELIC_DIR / "big"
POTION_DIR = ROOT / "Diviner" / "images" / "potions"

ITEM_ROWS = [
    [
        ("relic", "crystal_ball"),
        ("relic", "destined_crystal_ball"),
        ("relic", "clouded_lens"),
        ("relic", "brass_dowsing_rod"),
        ("relic", "blood_tablet"),
        ("relic", "piedra_del_sol"),
        ("relic", "knocked_compass"),
        ("relic", "marked_deck"),
    ],
    [
        ("relic", "prophets_quill"),
        ("relic", "sealed_envelope"),
        ("relic", "hourglass_of_mercy"),
        ("relic", "oracle_bone"),
        ("relic", "fixed_star_map"),
        ("relic", "last_prophecy"),
        ("relic", "velvet_pouch"),
        ("relic", "fated_contract"),
    ],
    [
        ("potion", "bottled_omen"),
        ("potion", "bitter_tea"),
        ("potion", "tar_of_dread"),
        ("potion", "brew_of_brew"),
        ("potion", "mercury_mirror"),
        ("potion", "condensed_misfortune"),
        ("potion", "starless_draught"),
        ("potion", "blood_of_the_martyr"),
    ],
]


def distance(a: tuple[int, int, int], b: tuple[int, int, int]) -> int:
    return max(abs(a[0] - b[0]), abs(a[1] - b[1]), abs(a[2] - b[2]))


def sample_background(image: Image.Image) -> tuple[int, int, int]:
    rgb = image.convert("RGB")
    width, height = rgb.size
    samples: list[tuple[int, int, int]] = []
    for x, y in (
        (0, 0),
        (width - 1, 0),
        (0, height - 1),
        (width - 1, height - 1),
        (width // 2, 0),
        (width // 2, height - 1),
        (0, height // 2),
        (width - 1, height // 2),
    ):
        samples.append(rgb.getpixel((x, y)))
    return tuple(sorted(channel)[len(channel) // 2] for channel in zip(*samples))  # type: ignore[return-value]


def remove_dark_specks(image: Image.Image, max_area: int = 36, max_luma: int = 82) -> Image.Image:
    rgba = image.convert("RGBA")
    width, height = rgba.size
    pixels = list(rgba.getdata())
    alpha = [pixel[3] for pixel in pixels]
    visited = bytearray(width * height)

    for start, start_alpha in enumerate(alpha):
        if start_alpha == 0 or visited[start]:
            continue

        component: list[int] = []
        luma_total = 0
        queue: deque[int] = deque([start])
        visited[start] = 1

        while queue:
            index = queue.popleft()
            component.append(index)
            red, green, blue, _ = pixels[index]
            luma_total += round(red * 0.2126 + green * 0.7152 + blue * 0.0722)
            x = index % width
            y = index // width

            for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if nx < 0 or nx >= width or ny < 0 or ny >= height:
                    continue
                next_index = ny * width + nx
                if alpha[next_index] and not visited[next_index]:
                    visited[next_index] = 1
                    queue.append(next_index)

        if len(component) <= max_area and luma_total / len(component) <= max_luma:
            for index in component:
                alpha[index] = 0

    cleaned = Image.new("L", (width, height), 0)
    cleaned.putdata(alpha)
    rgba.putalpha(cleaned)
    return rgba


def remove_connected_background(image: Image.Image, threshold: int = 24) -> Image.Image:
    rgb = image.convert("RGB")
    width, height = rgb.size
    pixels = list(rgb.getdata())
    background = sample_background(rgb)
    bg_like = bytearray(1 if distance(pixel, background) <= threshold else 0 for pixel in pixels)
    connected = bytearray(width * height)
    queue: deque[int] = deque()

    def enqueue(x: int, y: int) -> None:
        index = y * width + x
        if bg_like[index] and not connected[index]:
            connected[index] = 1
            queue.append(index)

    for x in range(width):
        enqueue(x, 0)
        enqueue(x, height - 1)
    for y in range(1, height - 1):
        enqueue(0, y)
        enqueue(width - 1, y)

    while queue:
        index = queue.popleft()
        x = index % width
        y = index // width
        if x > 0:
            enqueue(x - 1, y)
        if x < width - 1:
            enqueue(x + 1, y)
        if y > 0:
            enqueue(x, y - 1)
        if y < height - 1:
            enqueue(x, y + 1)

    alpha = Image.new("L", (width, height), 0)
    alpha.putdata([0 if connected[index] else 255 for index in range(width * height)])
    result = image.convert("RGBA")
    result.putalpha(alpha)
    return remove_dark_specks(result)


def fit_icon(image: Image.Image, size: int, padding: int) -> Image.Image:
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        return Image.new("RGBA", (size, size), (0, 0, 0, 0))

    cropped = image.crop(bbox)
    width, height = cropped.size
    max_width = size - padding * 2
    max_height = size - padding * 2
    scale = min(max_width / width, max_height / height)
    target = (max(1, round(width * scale)), max(1, round(height * scale)))
    resized = cropped.resize(target, Image.LANCZOS)

    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(resized, ((size - target[0]) // 2, (size - target[1]) // 2))
    return canvas


def save_outline(source: Image.Image, path: Path) -> None:
    alpha = source.getchannel("A")
    expanded = alpha.filter(ImageFilter.MaxFilter(7))
    outline = Image.new("RGBA", source.size, (9, 10, 18, 225))
    outline.putalpha(expanded.point(lambda value: min(value, 225)))
    path.parent.mkdir(parents=True, exist_ok=True)
    outline.save(path)


def crop_cell(sheet: Image.Image, row: int, col: int, inset: int = 2) -> Image.Image:
    width, height = sheet.size
    left = round(col * width / 8) + inset
    upper = round(row * height / 3) + inset
    right = round((col + 1) * width / 8) - inset
    lower = round((row + 1) * height / 3) - inset
    return sheet.crop((left, upper, right, lower))


def make_preview(icons: list[tuple[str, Image.Image]], path: Path) -> None:
    cols = 8
    cell = 128
    rows = (len(icons) + cols - 1) // cols
    preview = Image.new("RGBA", (cols * cell, rows * cell), (24, 27, 34, 255))
    for index, (_slug, icon) in enumerate(icons):
        x = (index % cols) * cell
        y = (index // cols) * cell
        preview.alpha_composite(icon, (x, y))
    preview.save(path)


def apply_sheet(sheet_path: Path, preview_path: Path) -> None:
    sheet = Image.open(sheet_path).convert("RGBA")
    RELIC_DIR.mkdir(parents=True, exist_ok=True)
    RELIC_BIG_DIR.mkdir(parents=True, exist_ok=True)
    POTION_DIR.mkdir(parents=True, exist_ok=True)

    preview_icons: list[tuple[str, Image.Image]] = []
    for row_index, row in enumerate(ITEM_ROWS):
        for col_index, (kind, slug) in enumerate(row):
            transparent = remove_connected_background(crop_cell(sheet, row_index, col_index))
            small = fit_icon(transparent, 128, 8)
            preview_icons.append((slug, small))

            if kind == "relic":
                small.save(RELIC_DIR / f"{slug}.png")
                save_outline(small, RELIC_DIR / f"{slug}_outline.png")
                fit_icon(transparent, 256, 16).save(RELIC_BIG_DIR / f"{slug}.png")
            else:
                small.save(POTION_DIR / f"{slug}.png")
                save_outline(small, POTION_DIR / f"{slug}_outline.png")

    make_preview(preview_icons, preview_path)


def main() -> None:
    parser = argparse.ArgumentParser(description="Split the Diviner relic/potion icon sheet into game assets.")
    parser.add_argument("sheet", type=Path, help="Path to a 3-row by 8-column relic/potion sheet.")
    parser.add_argument(
        "--preview",
        type=Path,
        default=Path("/tmp/diviner_item_sheet_split_preview.png"),
        help="Where to write a contact-sheet preview.",
    )
    args = parser.parse_args()
    apply_sheet(args.sheet, args.preview)
    print(f"Applied item sheet: {args.sheet}")
    print(f"Preview: {args.preview}")


if __name__ == "__main__":
    main()
