#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
from typing import Callable, Iterable

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
RELIC_DIR = ROOT / "Diviner" / "images" / "relics"
RELIC_BIG_DIR = RELIC_DIR / "big"
POTION_DIR = ROOT / "Diviner" / "images" / "potions"

AA = 4

DARK = (16, 18, 29, 255)
DARKER = (8, 10, 18, 255)
INK = (28, 30, 42, 255)
NAVY = (35, 42, 70, 255)
PURPLE = (54, 45, 91, 255)
TEAL = (86, 222, 224, 255)
TEAL_DARK = (49, 159, 155, 255)
GOLD = (232, 183, 66, 255)
GOLD_DARK = (151, 111, 44, 255)
CRIMSON = (197, 72, 76, 255)
CRIMSON_DARK = (104, 38, 48, 255)
IVORY = (235, 228, 205, 255)
STONE = (143, 136, 124, 255)
STONE_DARK = (80, 78, 82, 255)
SHADOW = (0, 0, 0, 85)


def rgba(color: tuple[int, int, int, int], alpha: int | None = None) -> tuple[int, int, int, int]:
    if alpha is None:
        return color
    return color[:3] + (alpha,)


class Icon:
    def __init__(self, size: int) -> None:
        self.size = size
        self.scale = size * AA / 128
        self.image = Image.new("RGBA", (size * AA, size * AA), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image)

    def p(self, value: float) -> int:
        return round(value * self.scale)

    def box(self, xy: tuple[float, float, float, float]) -> tuple[int, int, int, int]:
        return tuple(self.p(v) for v in xy)  # type: ignore[return-value]

    def pts(self, points: Iterable[tuple[float, float]]) -> list[tuple[int, int]]:
        return [(self.p(x), self.p(y)) for x, y in points]

    def line(
        self,
        points: Iterable[tuple[float, float]],
        fill: tuple[int, int, int, int],
        width: float = 3,
        joint: str = "curve",
    ) -> None:
        self.draw.line(self.pts(points), fill=fill, width=self.p(width), joint=joint)

    def polygon(
        self,
        points: Iterable[tuple[float, float]],
        fill: tuple[int, int, int, int],
        outline: tuple[int, int, int, int] | None = None,
        width: float = 2,
    ) -> None:
        pts = self.pts(points)
        self.draw.polygon(pts, fill=fill)
        if outline:
            self.draw.line(pts + [pts[0]], fill=outline, width=self.p(width), joint="curve")

    def ellipse(
        self,
        xy: tuple[float, float, float, float],
        fill: tuple[int, int, int, int],
        outline: tuple[int, int, int, int] | None = None,
        width: float = 2,
    ) -> None:
        self.draw.ellipse(self.box(xy), fill=fill, outline=outline, width=self.p(width))

    def rounded(
        self,
        xy: tuple[float, float, float, float],
        radius: float,
        fill: tuple[int, int, int, int],
        outline: tuple[int, int, int, int] | None = None,
        width: float = 2,
    ) -> None:
        self.draw.rounded_rectangle(
            self.box(xy),
            radius=self.p(radius),
            fill=fill,
            outline=outline,
            width=self.p(width),
        )

    def arc(
        self,
        xy: tuple[float, float, float, float],
        start: int,
        end: int,
        fill: tuple[int, int, int, int],
        width: float = 3,
    ) -> None:
        self.draw.arc(self.box(xy), start=start, end=end, fill=fill, width=self.p(width))

    def glow_ellipse(
        self,
        xy: tuple[float, float, float, float],
        fill: tuple[int, int, int, int],
        blur: float = 6,
    ) -> None:
        layer = Image.new("RGBA", self.image.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        d.ellipse(self.box(xy), fill=fill)
        layer = layer.filter(ImageFilter.GaussianBlur(self.p(blur)))
        self.image.alpha_composite(layer)

    def glow_polygon(
        self,
        points: Iterable[tuple[float, float]],
        fill: tuple[int, int, int, int],
        blur: float = 6,
    ) -> None:
        layer = Image.new("RGBA", self.image.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        d.polygon(self.pts(points), fill=fill)
        layer = layer.filter(ImageFilter.GaussianBlur(self.p(blur)))
        self.image.alpha_composite(layer)

    def shadow(self, xy: tuple[float, float, float, float]) -> None:
        self.ellipse(xy, SHADOW)

    def save(self, path: Path) -> Image.Image:
        path.parent.mkdir(parents=True, exist_ok=True)
        final = self.image.resize((self.size, self.size), Image.LANCZOS)
        final.save(path)
        return final


def star(cx: float, cy: float, outer: float, inner: float, points: int = 5, rotation: float = -90) -> list[tuple[float, float]]:
    import math

    result: list[tuple[float, float]] = []
    for i in range(points * 2):
        angle = math.radians(rotation + i * 180 / points)
        radius = outer if i % 2 == 0 else inner
        result.append((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius))
    return result


def diamond(cx: float, cy: float, rx: float, ry: float) -> list[tuple[float, float]]:
    return [(cx, cy - ry), (cx + rx, cy), (cx, cy + ry), (cx - rx, cy)]


def card_shape(x: float, y: float, w: float, h: float) -> list[tuple[float, float]]:
    return [(x + 5, y), (x + w, y + 3), (x + w - 4, y + h), (x, y + h - 4)]


def draw_crystal_ball(icon: Icon, destined: bool = False) -> None:
    icon.shadow((32, 96, 96, 114))
    icon.glow_ellipse((24, 17, 104, 97), rgba(TEAL, 70), 8)
    icon.ellipse((30, 20, 98, 88), rgba(TEAL, 64), IVORY, 3)
    icon.ellipse((38, 28, 90, 80), rgba(TEAL, 30), TEAL, 1.5)
    icon.line([(47, 48), (62, 32), (79, 49), (70, 70), (50, 66), (47, 48)], rgba(IVORY, 135), 1.5)
    icon.line([(36, 56), (92, 56)], rgba(TEAL, 150), 1.2)
    if destined:
        icon.ellipse((41, 45, 58, 62), GOLD, GOLD_DARK, 1.5)
        icon.polygon(diamond(78, 54, 9, 12), CRIMSON, CRIMSON_DARK, 1.5)
        icon.line([(50, 73), (64, 62), (78, 73)], rgba(GOLD, 180), 2)
    else:
        icon.polygon(diamond(64, 54, 8, 11), rgba(IVORY, 190), rgba(TEAL, 220), 1.5)
        icon.line([(45, 70), (62, 61), (84, 69)], rgba(GOLD, 160), 2)
    icon.polygon([(45, 88), (83, 88), (93, 106), (35, 106)], GOLD_DARK, INK, 2)
    icon.rounded((42, 101, 86, 111), 4, GOLD, INK, 2)


def relic_clouded_lens(icon: Icon) -> None:
    icon.shadow((30, 96, 100, 113))
    icon.glow_ellipse((19, 16, 90, 87), rgba(TEAL, 45), 7)
    icon.ellipse((24, 19, 86, 81), rgba(IVORY, 100), IVORY, 4)
    icon.ellipse((32, 27, 78, 73), rgba(TEAL, 65), TEAL_DARK, 2)
    icon.arc((35, 34, 72, 69), 190, 30, rgba(IVORY, 180), 3)
    icon.arc((43, 39, 76, 72), 200, 40, rgba(TEAL, 190), 2)
    icon.line([(77, 74), (103, 101)], GOLD_DARK, 10)
    icon.line([(77, 74), (103, 101)], GOLD, 5)


def relic_brass_dowsing_rod(icon: Icon) -> None:
    icon.shadow((33, 94, 96, 112))
    icon.glow_ellipse((42, 25, 86, 69), rgba(TEAL, 70), 7)
    icon.line([(64, 98), (64, 55), (36, 27)], GOLD_DARK, 10)
    icon.line([(64, 55), (93, 27)], GOLD_DARK, 10)
    icon.line([(64, 98), (64, 55), (36, 27)], GOLD, 5)
    icon.line([(64, 55), (93, 27)], GOLD, 5)
    icon.polygon(diamond(64, 52, 12, 15), TEAL, TEAL_DARK, 2)
    icon.line([(48, 75), (80, 75)], rgba(TEAL, 190), 2)


def relic_blood_tablet(icon: Icon) -> None:
    icon.shadow((33, 98, 96, 113))
    icon.rounded((33, 17, 95, 103), 8, STONE, INK, 3)
    icon.polygon([(37, 24), (90, 20), (87, 97), (42, 100)], rgba(STONE_DARK, 90), None)
    icon.line([(50, 37), (63, 30), (78, 40), (63, 55), (51, 48)], CRIMSON, 5)
    icon.line([(47, 67), (82, 62)], CRIMSON_DARK, 4)
    icon.line([(55, 80), (74, 82)], CRIMSON, 4)
    icon.ellipse((61, 56, 69, 68), CRIMSON)


def relic_piedra_del_sol(icon: Icon) -> None:
    icon.shadow((31, 99, 98, 114))
    icon.glow_ellipse((23, 20, 105, 102), rgba(GOLD, 55), 8)
    icon.polygon(star(64, 61, 47, 36, 12), GOLD_DARK, INK, 2)
    icon.ellipse((31, 28, 97, 94), GOLD, INK, 3)
    icon.ellipse((45, 42, 83, 80), rgba(TEAL, 190), TEAL_DARK, 2)
    icon.polygon(star(64, 61, 18, 8, 8), IVORY, GOLD_DARK, 1.5)


def relic_knocked_compass(icon: Icon) -> None:
    icon.shadow((30, 98, 100, 114))
    icon.glow_ellipse((23, 19, 105, 101), rgba(CRIMSON, 45), 8)
    icon.ellipse((27, 22, 101, 96), rgba(NAVY, 245), IVORY, 4)
    icon.ellipse((39, 34, 89, 84), rgba(DARK, 210), GOLD_DARK, 2)
    icon.line([(47, 74), (77, 42)], CRIMSON, 6)
    icon.line([(62, 62), (83, 71)], GOLD, 5)
    icon.line([(67, 34), (61, 43), (68, 48)], rgba(IVORY, 200), 2)
    icon.line([(38, 50), (47, 55), (41, 62)], rgba(CRIMSON, 160), 2)


def relic_marked_deck(icon: Icon) -> None:
    icon.shadow((28, 99, 101, 114))
    icon.polygon(card_shape(35, 29, 48, 66), rgba(IVORY, 230), INK, 2)
    icon.polygon(card_shape(45, 22, 48, 66), rgba(TEAL_DARK, 235), INK, 2)
    icon.polygon(card_shape(52, 31, 48, 66), rgba(IVORY, 230), INK, 2)
    icon.polygon(diamond(76, 60, 11, 15), GOLD, GOLD_DARK, 1.5)
    icon.line([(62, 82), (90, 84)], rgba(CRIMSON, 180), 3)


def relic_prophets_quill(icon: Icon) -> None:
    icon.shadow((29, 101, 101, 115))
    icon.glow_ellipse((23, 18, 97, 83), rgba(TEAL, 45), 7)
    icon.polygon([(34, 91), (76, 21), (96, 17), (83, 38), (46, 97)], IVORY, INK, 2)
    icon.line([(50, 88), (81, 34)], GOLD_DARK, 3)
    icon.line([(67, 35), (91, 21)], rgba(TEAL, 180), 2)
    icon.polygon([(38, 92), (30, 110), (51, 99)], INK, GOLD, 2)
    icon.line([(28, 109), (66, 103)], CRIMSON, 3)


def relic_sealed_envelope(icon: Icon) -> None:
    icon.shadow((25, 97, 103, 113))
    icon.rounded((25, 35, 103, 89), 6, IVORY, INK, 3)
    icon.line([(28, 39), (64, 67), (100, 39)], GOLD_DARK, 3)
    icon.line([(28, 87), (55, 63)], rgba(STONE_DARK, 180), 2)
    icon.line([(100, 87), (73, 63)], rgba(STONE_DARK, 180), 2)
    icon.ellipse((55, 55, 73, 73), CRIMSON, CRIMSON_DARK, 2)
    icon.polygon(star(64, 64, 8, 3.5, 5), GOLD)


def relic_hourglass(icon: Icon) -> None:
    icon.shadow((33, 99, 97, 115))
    icon.line([(43, 20), (85, 20), (72, 62), (85, 104), (43, 104), (56, 62), (43, 20)], GOLD_DARK, 5)
    icon.line([(48, 26), (80, 26), (68, 58), (80, 98), (48, 98), (60, 58), (48, 26)], rgba(IVORY, 75), 2)
    icon.polygon([(53, 32), (75, 32), (66, 55), (62, 55)], rgba(TEAL, 145), None)
    icon.polygon([(61, 70), (67, 70), (78, 94), (50, 94)], rgba(GOLD, 205), None)
    icon.line([(64, 56), (64, 71)], GOLD, 2)


def relic_oracle_bone(icon: Icon) -> None:
    icon.shadow((31, 100, 100, 115))
    icon.glow_ellipse((34, 18, 94, 98), rgba(CRIMSON, 42), 8)
    icon.polygon(
        [(44, 92), (49, 55), (41, 30), (51, 18), (65, 37), (80, 18), (89, 31), (76, 57), (83, 94), (64, 105)],
        IVORY,
        INK,
        3,
    )
    icon.line([(52, 42), (62, 51), (55, 65)], CRIMSON_DARK, 3)
    icon.line([(76, 36), (69, 50), (77, 63)], CRIMSON, 3)
    icon.polygon(diamond(65, 72, 7, 9), TEAL, TEAL_DARK, 1.5)


def relic_fixed_star_map(icon: Icon) -> None:
    icon.shadow((27, 99, 103, 115))
    icon.rounded((26, 28, 102, 92), 7, rgba(IVORY, 235), INK, 3)
    icon.ellipse((19, 27, 40, 47), rgba(IVORY, 230), INK, 2)
    icon.ellipse((88, 74, 109, 94), rgba(IVORY, 230), INK, 2)
    stars = [(47, 47), (62, 36), (79, 49), (72, 69), (52, 69)]
    icon.line(stars + [stars[0]], TEAL, 2)
    for x, y in stars:
        icon.polygon(star(x, y, 5, 2, 5), GOLD)


def relic_last_prophecy(icon: Icon) -> None:
    icon.shadow((27, 100, 103, 115))
    icon.glow_ellipse((28, 52, 100, 104), rgba(CRIMSON, 50), 8)
    icon.polygon([(34, 19), (94, 22), (90, 101), (39, 96), (30, 82), (38, 67)], rgba(IVORY, 230), INK, 3)
    icon.line([(46, 39), (82, 41)], rgba(STONE_DARK, 170), 3)
    icon.line([(45, 56), (85, 57)], rgba(STONE_DARK, 150), 3)
    icon.line([(43, 76), (88, 77)], CRIMSON, 5)
    icon.polygon(diamond(64, 91, 9, 7), TEAL, None)


def relic_velvet_pouch(icon: Icon) -> None:
    icon.shadow((31, 100, 99, 115))
    icon.glow_ellipse((38, 34, 90, 104), rgba(PURPLE, 80), 7)
    icon.polygon([(41, 46), (88, 45), (99, 93), (82, 109), (47, 108), (30, 93)], PURPLE, INK, 3)
    icon.rounded((40, 33, 88, 51), 8, rgba(CRIMSON_DARK, 220), INK, 2)
    icon.line([(36, 55), (93, 55)], GOLD, 4)
    icon.ellipse((54, 64, 74, 87), rgba(TEAL, 185), TEAL_DARK, 2)
    icon.polygon(star(82, 72, 9, 4, 5), GOLD)


def relic_fated_contract(icon: Icon) -> None:
    icon.shadow((27, 100, 102, 115))
    icon.glow_polygon([(30, 24), (97, 18), (91, 101), (35, 94)], rgba(GOLD, 45), 7)
    icon.polygon([(30, 24), (97, 18), (91, 101), (35, 94)], rgba(IVORY, 235), INK, 3)
    icon.line([(45, 40), (83, 37)], rgba(STONE_DARK, 170), 3)
    icon.line([(44, 55), (75, 53)], rgba(STONE_DARK, 140), 3)
    icon.line([(43, 70), (80, 67)], rgba(TEAL_DARK, 190), 3)
    icon.ellipse((64, 74, 88, 98), CRIMSON, CRIMSON_DARK, 2)
    icon.polygon(star(76, 86, 10, 4, 5), GOLD)


def draw_bottle_base(
    icon: Icon,
    body: tuple[int, int, int, int],
    liquid: tuple[int, int, int, int],
    neck: str = "round",
) -> None:
    icon.shadow((34, 100, 94, 116))
    icon.glow_ellipse((32, 28, 96, 104), rgba(liquid, 55), 8)
    if neck == "wide":
        icon.rounded((50, 17, 78, 39), 6, rgba(IVORY, 220), INK, 2)
    else:
        icon.rounded((54, 17, 74, 42), 5, rgba(IVORY, 220), INK, 2)
    icon.rounded((47, 33, 81, 48), 8, rgba(IVORY, 230), INK, 2)
    icon.rounded((35, 42, 93, 105), 17, body, INK, 3)
    icon.rounded((42, 58, 86, 99), 14, liquid, None, 0)
    icon.arc((42, 47, 86, 91), 12, 168, rgba(IVORY, 95), 2)


def potion_bottled_omen(icon: Icon) -> None:
    draw_bottle_base(icon, rgba(NAVY, 235), rgba(TEAL, 170))
    icon.polygon(diamond(64, 72, 10, 13), GOLD, GOLD_DARK, 1.5)
    icon.polygon(diamond(49, 77, 7, 9), CRIMSON, CRIMSON_DARK, 1.2)
    icon.polygon(diamond(79, 77, 7, 9), TEAL, TEAL_DARK, 1.2)


def potion_bitter_tea(icon: Icon) -> None:
    icon.shadow((28, 100, 102, 116))
    icon.glow_ellipse((34, 34, 95, 100), rgba(GOLD, 42), 8)
    icon.rounded((35, 49, 91, 92), 12, IVORY, INK, 3)
    icon.ellipse((42, 48, 84, 64), rgba(GOLD_DARK, 210), GOLD, 2)
    icon.arc((84, 58, 106, 82), -80, 95, IVORY, 5)
    icon.line([(49, 35), (44, 25)], rgba(TEAL, 170), 3)
    icon.line([(64, 34), (66, 20)], rgba(TEAL, 170), 3)
    icon.line([(78, 35), (84, 25)], rgba(TEAL, 170), 3)


def potion_tar_of_dread(icon: Icon) -> None:
    draw_bottle_base(icon, rgba(DARK, 250), rgba(CRIMSON_DARK, 230), "wide")
    icon.rounded((44, 61, 84, 95), 12, rgba(DARKER, 245), None, 0)
    icon.polygon(diamond(64, 75, 10, 14), CRIMSON, CRIMSON_DARK, 1.5)
    icon.line([(50, 50), (45, 35), (55, 27)], rgba(CRIMSON, 170), 4)
    icon.line([(78, 50), (86, 36), (78, 29)], rgba(CRIMSON, 140), 4)


def potion_brew_of_brew(icon: Icon) -> None:
    draw_bottle_base(icon, rgba(PURPLE, 235), rgba(TEAL_DARK, 210), "wide")
    icon.rounded((53, 57, 76, 91), 8, rgba(GOLD, 210), INK, 2)
    icon.rounded((58, 48, 71, 61), 4, rgba(IVORY, 220), INK, 1.5)
    icon.ellipse((59, 66, 70, 82), rgba(TEAL, 190), None)


def potion_mercury_mirror(icon: Icon) -> None:
    icon.shadow((30, 101, 99, 116))
    icon.glow_ellipse((30, 25, 98, 99), rgba(TEAL, 55), 8)
    icon.ellipse((32, 21, 96, 85), rgba(IVORY, 220), INK, 3)
    icon.ellipse((41, 30, 87, 76), rgba(TEAL, 100), TEAL_DARK, 2)
    icon.line([(49, 65), (77, 37)], rgba(IVORY, 150), 3)
    icon.line([(64, 84), (64, 106)], GOLD_DARK, 8)
    icon.line([(64, 84), (64, 106)], GOLD, 4)
    icon.rounded((52, 101, 76, 112), 4, GOLD, INK, 2)


def potion_condensed_misfortune(icon: Icon) -> None:
    draw_bottle_base(icon, rgba(CRIMSON_DARK, 240), rgba(CRIMSON, 220))
    icon.polygon(diamond(64, 74, 15, 22), rgba(DARKER, 230), INK, 1.5)
    icon.line([(54, 65), (66, 74), (59, 88)], rgba(TEAL, 170), 3)
    icon.line([(75, 62), (68, 78), (78, 89)], rgba(TEAL, 150), 3)


def potion_starless_draught(icon: Icon) -> None:
    draw_bottle_base(icon, rgba(DARKER, 250), rgba(NAVY, 235))
    icon.ellipse((49, 58, 79, 88), DARKER, TEAL, 2)
    icon.polygon(star(64, 73, 15, 6, 5), rgba(TEAL, 40), TEAL_DARK, 1.5)
    icon.polygon(star(83, 43, 8, 3, 5), GOLD)


def potion_blood_of_martyr(icon: Icon) -> None:
    draw_bottle_base(icon, rgba(IVORY, 225), rgba(CRIMSON, 220), "wide")
    icon.polygon(star(64, 58, 16, 7, 6), GOLD, None)
    icon.ellipse((52, 68, 76, 96), rgba(CRIMSON_DARK, 220), CRIMSON, 2)
    icon.line([(64, 48), (64, 75)], rgba(IVORY, 150), 3)


RELICS: dict[str, Callable[[Icon], None]] = {
    "crystal_ball": lambda icon: draw_crystal_ball(icon, False),
    "destined_crystal_ball": lambda icon: draw_crystal_ball(icon, True),
    "clouded_lens": relic_clouded_lens,
    "brass_dowsing_rod": relic_brass_dowsing_rod,
    "blood_tablet": relic_blood_tablet,
    "piedra_del_sol": relic_piedra_del_sol,
    "knocked_compass": relic_knocked_compass,
    "marked_deck": relic_marked_deck,
    "prophets_quill": relic_prophets_quill,
    "sealed_envelope": relic_sealed_envelope,
    "hourglass_of_mercy": relic_hourglass,
    "oracle_bone": relic_oracle_bone,
    "fixed_star_map": relic_fixed_star_map,
    "last_prophecy": relic_last_prophecy,
    "velvet_pouch": relic_velvet_pouch,
    "fated_contract": relic_fated_contract,
}

POTIONS: dict[str, Callable[[Icon], None]] = {
    "bottled_omen": potion_bottled_omen,
    "bitter_tea": potion_bitter_tea,
    "tar_of_dread": potion_tar_of_dread,
    "brew_of_brew": potion_brew_of_brew,
    "mercury_mirror": potion_mercury_mirror,
    "condensed_misfortune": potion_condensed_misfortune,
    "starless_draught": potion_starless_draught,
    "blood_of_the_martyr": potion_blood_of_martyr,
}


def render(draw_fn: Callable[[Icon], None], size: int) -> Image.Image:
    icon = Icon(size)
    draw_fn(icon)
    return icon.image.resize((size, size), Image.LANCZOS)


def save_outline(source: Image.Image, path: Path) -> None:
    alpha = source.getchannel("A")
    expanded = alpha.filter(ImageFilter.MaxFilter(7))
    outline = Image.new("RGBA", source.size, (9, 10, 18, 225))
    outline.putalpha(expanded.point(lambda value: min(value, 225)))
    path.parent.mkdir(parents=True, exist_ok=True)
    outline.save(path)


def save_asset_set() -> None:
    RELIC_DIR.mkdir(parents=True, exist_ok=True)
    RELIC_BIG_DIR.mkdir(parents=True, exist_ok=True)
    POTION_DIR.mkdir(parents=True, exist_ok=True)

    for slug, draw_fn in RELICS.items():
        small = render(draw_fn, 128)
        small.save(RELIC_DIR / f"{slug}.png")
        save_outline(small, RELIC_DIR / f"{slug}_outline.png")
        render(draw_fn, 256).save(RELIC_BIG_DIR / f"{slug}.png")

    for slug, draw_fn in POTIONS.items():
        small = render(draw_fn, 128)
        small.save(POTION_DIR / f"{slug}.png")
        save_outline(small, POTION_DIR / f"{slug}_outline.png")


def make_contact_sheet() -> Path:
    thumbs: list[tuple[str, Image.Image]] = []
    for slug, draw_fn in RELICS.items():
        thumbs.append((slug, render(draw_fn, 96)))
    for slug, draw_fn in POTIONS.items():
        thumbs.append((slug, render(draw_fn, 96)))

    cols = 8
    cell = 132
    rows = (len(thumbs) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell, rows * cell), (18, 20, 30, 255))
    for index, (_slug, image) in enumerate(thumbs):
        x = (index % cols) * cell + 18
        y = (index // cols) * cell + 10
        sheet.alpha_composite(image, (x, y))

    path = Path("/tmp/diviner_item_art_contact_sheet.png")
    sheet.save(path)
    return path


def main() -> None:
    save_asset_set()
    preview = make_contact_sheet()
    print(f"Generated {len(RELICS)} relics and {len(POTIONS)} potions.")
    print(f"Preview: {preview}")


if __name__ == "__main__":
    main()
