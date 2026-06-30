# Diviner Minimal Art v1

Generated on 2026-06-29 with a local Python/Pillow drawing script. The assets are flat/vector-like PNGs made from deterministic geometry, fixed coordinates, and a compact palette. No network calls, random sources, or AI image generation were used.

## Dimensions

| Asset group | Dimensions |
| --- | --- |
| `Diviner/images/card_portraits/*.png` | 250x190 |
| `Diviner/images/card_portraits/big/*.png` | 500x380 |
| `Diviner/images/relics/*.png` | 128x128 |
| `Diviner/images/relics/big/*.png` | 256x256 |
| `Diviner/images/charui/big_energy.png` | 64x64 |
| `Diviner/images/charui/text_energy.png` | 24x24 |
| `Diviner/images/character_icon_diviner.png` | 128x128 |
| `Diviner/images/map_marker_diviner.png` | 96x96 |

`Diviner/mod_image.png` already existed as a generic placeholder and was left unchanged in this pass.

## Palette

| Role | Hex |
| --- | --- |
| Dark ground | `#14171f`, `#1f232d`, `#0b0d13` |
| Readable foreground | `#ebe4cd` |
| Good omen / fortune accent | `#e8b742` |
| Divination / fate accent | `#4dbcae` |
| Bad omen / misfortune accent | `#c5484c` |

The batch intentionally reuses one dark base and three accents. Future v1 additions should stay inside this palette unless a card truly needs a new state color.

## Method

- Rendered locally with Pillow using primitive shapes only: lines, polygons, ellipses, arcs, and rounded rectangles.
- Rendered at 4x or 5x scale, then downsampled with Lanczos filtering for anti-aliased edges.
- Used one large central symbol per asset, no text labels, and minimal secondary marks.
- Preserved the existing filename conventions and dimensions for every existing asset.
- Added common-card portraits in both normal and `big` sizes so future implemented commons do not fall back to `mod_image.png`.

## Batch Contents

Implemented and generic card portraits:

`card`, `balance`, `divination_of_woes`, `fortune`, `misfortune`, `escape_from_destiny`, `palm_strike`, `destiny_fall`, `crossed_lines`, `thread_cut`, `wax_seal`, `misread_strike`, `eclipse_jab`, `forewarned_blow`, `destined_lance`, `read_the_room`, `insurance`, `bad_feeling`, `palm_reading`, `lucky_break`, `mark_calendar`, `false_alarm`, `omens_align`, `skeptics_charm`, `reconsult`, `narrow_escape`, `thread_pull`, `line_of_fate`, `tea_leaves`, `ward_sign`, `smoke_and_mirrors`, `small_ritual`

Documented common-card prefill portraits:

`consult_the_stars`, `falling_star`, `ill_fated_cut`, `star_needle`

Relic and UI assets:

`relic`, `relic_outline`, `crystal_ball`, `crystal_ball_outline`, `character_icon_diviner`, `map_marker_diviner`, `big_energy`, `text_energy`

## Thumbnail Readability

The card portraits were checked as a 75x57 thumbnail sheet, near in-hand/reward readability scale. The relic, character, map marker, and energy assets were checked together at small display sizes, including the 24x24 text energy icon. The intended read is a single high-contrast silhouette first, then the omen color second.

For future additions:

- Use one iconic object, sign, or omen rather than scenic compositions.
- Keep the main shape broad enough to survive a 60-80 px thumbnail.
- Avoid adding labels, tiny stars, dense linework, or extra accent colors.
- Verify the asset at normal in-game display size, not only at source resolution.
