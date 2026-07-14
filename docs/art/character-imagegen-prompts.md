# Diviner Character Asset Image Generation Prompts

These prompts are based on the attached base Diviner design: a hooded, faceless seer in dark navy-purple layered robes, cyan trim and eye glow, a floating cyan crystal ball, and a slender staff with a gold star tip.

Code-loaded character assets are referenced from `DivinerCode/Character/Diviner.cs`, `DivinerCode/Character/DivinerCharacterSelectEntry.cs`, and `DivinerCode/Character/DivinerCardPool.cs`. Two existing character-folder assets are included as supporting prompts because they already exist in the project and are natural replacement targets.

## Shared Character Prompt

Use the attached base character design as the strict character reference. Keep the hood shape, faceless dark mask, small cyan glowing eyes, angular layered robe panels, cyan glowing trim, floating crystal ball, and star-tipped staff. Clean stylized fantasy game art, polished 2D illustration, crisp silhouette, readable at small in-game size, dark navy and violet robes, cyan magic glow, muted gold star accent. Transparent background unless the asset prompt asks for a simple scene. No text, no letters, no numbers, no UI frame, no watermark, no extra characters, no gore, no photorealism.

## Required Character Scene Assets

### `Diviner/images/character/diviner_combat_idle.png`

Target size: 512x512.

```text
Full-body combat idle sprite of the Diviner, transparent background. The Diviner stands in a calm three-quarter pose facing slightly right, hood lowered over a faceless dark mask with two cyan glowing eyes. One hand is held forward under a floating cyan crystal ball with faint geometric fate lines inside; the other hand rests near a slender staff with a small gold star tip. Dark navy-purple layered robes with angular panels and cyan glowing trim, cloak falling in simple readable shapes. Balanced stance, quiet mystical confidence, soft cyan rim light, clean silhouette, no ground platform, no text, no UI.
```

### `Diviner/images/character/diviner_character_select.png`

Target size: 1024x768.

```text
Character select scene art featuring the Diviner, transparent or very subtle dark vignette background. The Diviner stands centered on a cracked stone platform, full body visible, slightly larger and more ceremonial than the combat sprite. The floating cyan crystal ball hovers above one open hand, the star-tipped staff is planted beside them, and cyan fate lines arc gently around the hood and shoulders. Leave comfortable empty space around the character for game UI. Dark navy-purple robes, angular layered cloak, cyan trim, small glowing eyes, muted gold star accent, polished 2D fantasy illustration, no text, no card elements, no UI.
```

## Supporting Character Scene Assets

### `Diviner/images/character/diviner_combat_splash.png`

Target size: 768x768.

```text
Dramatic square combat splash illustration of the Diviner, transparent background. The Diviner is in an active prophecy-casting pose, cloak sweeping diagonally, one hand pushing a bright cyan crystal ball forward while the star-tipped staff draws a gold-cyan arc behind them. Stronger motion and lighting than the idle sprite, but still matching the same hooded faceless design and layered navy-purple robe armor. Main silhouette readable, cyan magic glow concentrated around the crystal ball and eyes, no text, no UI, no background clutter.
```

### `Diviner/images/character/diviner_character_select_portrait.png`

Target size: 512x512.

```text
Square portrait of the Diviner from chest up, transparent background or very soft dark vignette. Hooded faceless mask with two cyan glowing eyes, cyan trim tracing the hood edge and collar, one hand partly visible holding a floating cyan crystal ball near the lower right of the portrait. The star-tipped staff appears as a slim diagonal shape in the background. Calm mysterious expression through posture only, dark navy-purple robes, crisp icon-like silhouette, polished 2D fantasy portrait, no text, no UI.
```

## Future Non-Combat Character Hooks

The game/BaseLib hooks for these surfaces are animation-resource paths on the character model:

- Shop character: `CustomMerchantAnimPath`, consumed by `NMerchantCharacter`.
- Rest-site character: `CustomRestSiteAnimPath`, consumed by `NRestSiteCharacter`.

Vanilla rest-site characters use Spine-style animation resources with source art split into body-part atlases. The prompts below are for the source character art or source sprite-sheet art to build those animation resources. Use transparent backgrounds and preserve separated, readable body/prop shapes if the output will be cut into an atlas.

### `Diviner/images/character/diviner_merchant.png`

Target surface: shop/merchant room character art for `CustomMerchantAnimPath`.

Recommended source size: 1024x1024 or larger, transparent background. If producing an animation atlas, keep major pieces separable: hood/head, torso, cloak, left arm, right arm, hands, crystal ball, staff, shadow, small glow effects.

```text
Shop/merchant-room character art for the Diviner, using the attached base design as strict reference. The Diviner stands in a calm bargaining pose, three-quarter view facing slightly toward the player, hooded faceless mask with two cyan glowing eyes. One hand presents a floating cyan crystal ball as if offering a forecast for sale; the other hand rests on or lightly holds the slender staff with a muted gold star tip. The robe should feel like a mysterious shop visitor: dark navy-purple layered cloak, cyan glowing trim, a few small fortune-telling trinkets or sealed omen tags hanging from the belt, but no readable text. Transparent background, full body, strong silhouette, soft cyan underlighting from the crystal ball, subtle gold accent from the staff star. Leave the lower body and cloak simple enough to animate or cut into body parts. No merchant NPC, no counter, no coins, no card UI, no text, no frame.
```

### `Diviner/images/character/diviner_rest_site.png`

Target surface: rest-site character art for `CustomRestSiteAnimPath`.

Recommended source size: 1024x1024 or larger, transparent background. If producing an animation atlas, keep major pieces separable: hood/head, torso, cloak, seated legs, left arm, right arm, hands, crystal ball, staff, shadow, fire/crystal glow overlays.

```text
Rest-site character art for the Diviner, using the attached base design as strict reference. The Diviner sits quietly beside an implied campfire, three-quarter view, cloak pooled around them in angular navy-purple folds. Their hooded faceless mask tilts down toward a floating cyan crystal ball hovering between both hands, with faint fate-line geometry inside the orb. The star-tipped staff rests diagonally beside them or is planted in the ground just behind the shoulder, its muted gold star catching a small warm fire highlight. Mood is peaceful, watchful, and prophetic: cyan crystal glow mixed with soft orange campfire rim light, relaxed posture, no combat aggression. Transparent background, full seated body, readable silhouette, simple broad shapes suitable for rest-site animation. No actual campfire background unless requested, no UI, no text, no card frame, no extra characters.
```

## Required Character UI Assets

### `Diviner/images/charui/char_select_diviner.png`

Target size: 132x195.

```text
Small vertical character select button icon, transparent background. Simplified full-body Diviner silhouette fitted inside a narrow vertical crop, hooded head and cyan glowing eyes clearly visible, floating cyan crystal ball near one hand, star-tipped staff rising along one side. Dark navy-purple cloak with cyan trim, high contrast, readable at very small size, no text, no UI frame, no background scene.
```

### `Diviner/images/charui/char_select_diviner_locked.png`

Target size: 132x195.

```text
Locked version of the vertical character select button icon, transparent background. Same pose and silhouette as the unlocked Diviner icon, but darker, desaturated, and partially shadowed, with only a faint cyan eye glow and dim crystal ball. Keep the hood, staff, and robe shape readable at small size. No lock symbol, no text, no UI frame, no background scene.
```

### `Diviner/images/character_icon_diviner.png`

Target size: 128x128.

```text
Square character icon portrait, transparent background. Close crop of the Diviner hood and upper chest, faceless dark mask with two cyan glowing eyes, cyan hood trim forming a strong readable outline, a small hint of the floating crystal ball glow at the lower edge. Bold simple silhouette for top-bar or roster use, dark navy-purple and cyan palette, no text, no UI frame, no extra props beyond the subtle crystal glow.
```

### `Diviner/images/map_marker_diviner.png`

Target size: 96x96.

```text
Small map marker icon for the Diviner, transparent background. Iconic simplified symbol combining the hood silhouette, a tiny cyan crystal ball, and a muted gold star point from the staff. Strong single-shape read, thick clean contours, dark navy-purple fill with cyan trim, readable at map scale. No text, no route lines, no UI frame, no detailed full body.
```

### `Diviner/images/charui/big_energy.png`

Target size: 64x64.

```text
Large Diviner energy icon, transparent background. A bright cyan faceted crystal orb with thin geometric fate-line facets inside, surrounded by a small muted gold star glint and subtle navy shadow. Clean circular silhouette, high contrast, readable at 64x64, matching the Diviner crystal ball motif. No text, no numbers, no UI frame.
```

### `Diviner/images/charui/text_energy.png`

Target size: 24x24.

```text
Tiny inline Diviner energy icon, transparent background. Extremely simplified cyan crystal spark or small faceted orb with one gold pixel-like star glint, thick clean silhouette, readable at 24x24. Use only two or three broad shapes, no fine linework, no text, no numbers, no UI frame.
```

## Optional Project Icon

### `Diviner/mod_image.png`

Target size: use the current project icon size.

```text
Compact mod icon for The Diviner, square crop, transparent or dark vignette background. Center the Diviner's hooded faceless head above a glowing cyan crystal ball, with the gold star tip of the staff peeking behind the hood. High contrast, readable as a launcher/mod-list icon, dark navy-purple robes, cyan trim and eyes, muted gold accent, no text, no UI frame.
```
