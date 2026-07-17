# Multiplayer Testing Branch Plan

Branch: `mp-testing-do-not-merge`

This branch is for experimental multiplayer compatibility work. Do not merge, delete, or deploy it until the multiplayer scenarios below have been exercised and the branch is explicitly promoted.

## Current Implementation Status

- Done: branch marker and severity plan added in this document.
- Done: Destiny and luck Destiny are player-scoped with legacy run-level fallback.
- Done: combat runtime state is player-scoped for Foretell queues, Dredge countdown, Revelation/start-of-combat flags, combat divination count, Escape costs, one-shot bonuses, and temporary fated/retained/free-card state.
- Done: card, relic, potion, status sync, reward luck, and HUD call sites have been migrated to owner/player-aware Destiny and combat-runtime APIs where currently identified.
- Done: hand-rolled `Random.Shared` gameplay rolls were replaced with STS2 run RNG streams for combat targets, combat card selection, and forecast selection.
- In progress: divination records are now player-scoped for new saves and key gameplay/HUD consumers, with legacy run-level compatibility shims still present.
- Still required: live multiplayer validation, save/load validation across host/client, and review of BaseLib `CardPilePosition.Random`/generated-card insertion behavior under multiplayer.

## Severity-Ordered Implementation Plan

1. Player-scope Destiny state.
   - Replace process-wide Destiny reads/writes with player-aware APIs.
   - Keep compatibility shims temporarily so the migration can happen in slices.
   - Persist player Destiny and luck Destiny separately, with legacy run-level fallback for old saves.

2. Player-scope combat runtime state.
   - Move Foretell queues, Dredge countdown, Revelation/start-of-combat flags, divination count, and one-shot bonuses into per-player combat state.
   - Leave card-instance keyed state alone unless it proves to leak across players.

3. Player-scope divination records.
   - Decide whether records are shared-party knowledge or Diviner-owned knowledge.
   - If owned, store owner slot/id on records and filter all counts/consumers by player.

4. Deterministic multiplayer RNG.
   - Replace gameplay `Random.Shared` uses with STS2/player/combat RNG or synchronized command choices.
   - Audit random targets, random hand/draw selection, and forecast category selection.

5. Reward and map hooks.
   - Apply Destiny reward modifiers only for the player whose reward is being generated.
   - Define behavior for shared map/reward rolls before enabling in multiplayer.

6. UI-only overlays.
   - Scope HUD/overlays to the local Diviner player.
   - Keep visual animation cosmetic and avoid blocking synced model commands.

## Manual Multiplayer Scenarios

- Diviner plus non-Diviner: Destiny, rewards, HUD, and potion/map odds affect only the Diviner.
- Two Diviners: each changes Destiny independently, queues Foretell independently, and resolves Dredge/Revelation independently.
- Save/load with changed Destiny and recorded divinations.
- Reward screen with Marked Deck, Many Futures, positive Destiny, and negative Destiny.
- Random effects: Mercury Mirror, Hexagram, Apocalypse, Resonation of Fate.
- Dredge: one Diviner reaches 0 Destiny, shuffles Escape from Destiny, ticks countdown, plays Escape, and dies at 0 without affecting another player.
