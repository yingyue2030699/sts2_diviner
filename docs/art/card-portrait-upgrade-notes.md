# Diviner Card Portrait Upgrade Notes

Updated on 2026-06-30 for the starter/generated/basic portrait pass.

## 2026-06-30 Duplicate Reading/Future Follow-up

This pass removed the remaining exact duplicate portraits in both the normal and `big/` card portrait folders. The following cards received distinct minimalist replacements while preserving the existing palette, dimensions, and one-symbol readability rule:

`the_last_word`, `white_room`, `threadcutter`, `loaded_reading`, `many_futures`, `perfect_forecast`, `haruspex_method`, `the_written_hour`

The replacements focus on broad symbols that stay legible in reward/library thumbnails: a terminal omen line, white-room doorway, thread-cutting scissors, layered reading pages, branching timelines, target-star forecast, ritual bowl, and clock-star mark.

## 2026-06-30 Duplicate Cluster Follow-up

This pass replaced the remaining exact duplicate portraits found in the clusters below, preserving the existing `250x190` normal and `500x380` big dimensions:

`consult_the_stars`, `forewarned_blow`, `falling_star`, `ill_fated_cut`, `thread_cut`, `insurance`, `narrow_escape`

The replacements keep one central object or omen, one quiet accent at most, and the same dark Diviner palette. The original companion portraits in each cluster were left unchanged so the cards now read as related but distinct.

Initial character art was also added under `Diviner/images/character/`:

`diviner_combat_idle`, `diviner_combat_splash`, `diviner_character_select`, `diviner_character_select_portrait`

These are deterministic local raster assets with a hooded diviner silhouette, crystal ball, staff/crescent motif, and transparent backgrounds for combat/splash use where appropriate.

## Placeholder Duplicates Found

The first obvious duplicated card portrait clusters were:

| Shared art | Normal portraits | Big portraits |
| --- | --- | --- |
| Palm strike hand | `strike_diviner`, `palm_strike` | `strike_diviner`, `palm_strike` |
| Ward card | `defend_diviner`, `narrow_escape` | `defend_diviner`, `narrow_escape` |
| Cut thread | `misread_strike`, `ill_fated_cut`, `thread_cut` | `misread_strike`, `ill_fated_cut`, `thread_cut` |
| Falling star | `falling_star`, `destiny_fall` | `falling_star`, `destiny_fall` |
| Lance | `destined_lance`, `forewarned_blow` | `destined_lance`, `forewarned_blow` |
| Lucky break | `lucky_break`, `insurance` | `lucky_break`, `insurance` |
| Consult cards | `reconsult`, `consult_the_stars` | `reconsult`, `consult_the_stars` |

This pass replaced the starter/generated/basic portraits first:

`strike_diviner`, `defend_diviner`, `balance`, `divination_of_woes`, `fortune`, `misfortune`, `escape_from_destiny`

## Current Art Direction

- Preserve `250x190` normal portraits and `500x380` `big/` portraits.
- Use one central symbol and at most one secondary motion or omen mark.
- Keep the foreground to one tight color family plus a near highlight.
- Use dark, quiet backgrounds with mild vignette only.
- Avoid text, borders, dense scene detail, tiny stars, and extra props.

## Future Imagegen Prompts

These prompts are written for a future direct image generation pass if a project-copyable output path is available.

### Strike Diviner

Polished simple fantasy deckbuilder card portrait, landscape crop. One luminous ivory diviner hand making a forward palm strike, with one muted gold crescent motion trail behind it. Plain dark indigo background, soft painterly glow, no border, no text, no face, no extra hands, no small details. Readable as a hand and crescent at thumbnail size.

### Defend Diviner

Polished simple fantasy deckbuilder card portrait, landscape crop. One upright pale ward tablet or shield with a single teal upward ward mark. Plain dark blue-green background, soft glow, no border, no text, no extra symbols, no scene. Readable as defense at thumbnail size.

### Balance

Polished simple fantasy deckbuilder card portrait, landscape crop. One clean balance scale with two pale hanging pans and a muted teal beam. Plain dark blue-green background, soft glow, no border, no text, no coins, no extra objects. Readable as scales at thumbnail size.

### Divination of Woes

Polished simple fantasy deckbuilder card portrait, landscape crop. One pale omen eye with three simple muted red falling omen drops beneath it. Plain dark wine-indigo background, soft red glow, no border, no text, no extra eyes, no complex occult scene. Readable as a warning eye at thumbnail size.

### Fortune

Polished simple fantasy deckbuilder card portrait, landscape crop. One warm gold fortune coin with a dark star-shaped cutout, plus a faint gold crescent glint. Plain dark indigo background, soft glow, no border, no text, no extra coins, no clutter. Readable as a lucky coin at thumbnail size.

### Misfortune

Polished simple fantasy deckbuilder card portrait, landscape crop. One muted crimson diamond talisman split by a dark crack. Plain dark wine background, soft red glow, no border, no text, no extra shards, no blood, no scene. Readable as a broken omen at thumbnail size.

### Escape From Destiny

Polished simple fantasy deckbuilder card portrait, landscape crop. One broken teal portal arc with a single pale upward escape arrow passing through it. Plain dark blue-green background, soft glow, no border, no text, no extra cards, no character. Readable as escape at thumbnail size.
