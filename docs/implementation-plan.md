# Diviner Implementation Plan

Current implementation status is tracked in `docs/plan-status.md`.

## Architecture

Use a small set of services instead of scattering fate logic through cards:

- `DivinerRunState`: saved destiny value, divination records, category cooldowns, and per-run flags.
- `DestinyService`: clamp, change, query Good/Bad Omen, fire UI refresh.
- `DivinationService`: weighted provider selection, category cooldowns, duplicate suppression, and Crystal Ball record text.
- `ForecastProvider` implementations: boss, ancient, relic queue, event, elite.
- `DivinerCombatRuntime`: per-combat fields such as Dredge countdown, Escape cost tax, first-turn Enlightenment free-card IDs, and whether start-of-combat effects fired.
- `DestinyHud`: combat-first UI, later top-bar integration.

## Phase 1: Character Foundation

Files to add:

- `DivinerCode/Character/Diviner.cs`
- `DivinerCode/Character/DivinerCardPool.cs`
- `DivinerCode/Character/DivinerRelicPool.cs`
- `DivinerCode/Character/DivinerPotionPool.cs`
- `DivinerCode/Cards/DivinerCard.cs`
- `DivinerCode/Relics/DivinerRelic.cs`
- `DivinerCode/Potions/DivinerPotion.cs`
- `DivinerCode/Extensions/StringExtensions.cs`

Deliverables:

- Diviner appears as a selectable character.
- Starting deck: 4 Strike, 4 Defend, Balance, Omen of Woes.
- Starting relic: Crystal Ball.
- Placeholder card/relic/potion images load.
- `./scripts/build.sh` passes.

## Phase 2: Destiny Runtime

Deliverables:

- New runs start at destiny 3.
- Destiny is saved/loaded.
- Helper methods expose `BadOmen` for 0-2 and `GoodOmen` for 3-5.
- Helper methods expose `Dredge` for Destiny 0 and `Enlightenment` for Destiny 5.
- Combat HUD displays destiny and omen label.
- Debug card or console helper can set destiny for testing.

Quality gates:

- New run defaults to 3.
- Combat start at each value 0-5 shows correct omen.
- Destiny cannot go below 0 or above 5.
- Common and Uncommon cards do not casually change Destiny; direct changes are intentionally scarce and high-signal.

## Phase 3: Starter Mechanics

Deliverables:

- `Balance`: 1 cost, upgrades to 0. Good Omen: lose 1 Destiny, divinate, draw 1. Bad Omen: requires 2 extra energy, gain 1 Destiny. Exhaust.
- `Omen of Woes`: 1 cost. Foretell: deal 9 damage to all enemies and apply 1 Weak and 1 Vulnerable. Upgrade damage to 13.
- `Fortune`: generated 0 cost, Retain, draw 2, Exhaust.
- `Misfortune`: generated 3 cost, lose 3 HP, deal 15 damage to all enemies, autoplayed at end of turn.
- Crystal Ball adds Fortune or Misfortune at combat start.

Open balance choice:

- Crystal Ball should probably use a Destiny-weighted roll: Dredge/low Destiny mostly Misfortune, middle Destiny mixed, and Enlightenment/high Destiny mostly Fortune.

## Phase 4: Extremes

Deliverables:

- `Dredge`: the first time Destiny reaches 0 each combat, start Countdown of Destiny 3, shuffle 3 Escape from Destiny into the draw pile, and shuffle 3 into the discard pile.
- `Countdown of Destiny`: loses 1 at end of player turn; when it reaches 0, player is defeated.
- `Escape from Destiny`: 1 cost generated-only card; increase Countdown by 1; Exhaust. Upgrades to 0 cost.
- `Enlightenment`: at Destiny 5, start combat by choosing 3 cards from draw pile, putting them into hand, and making them free this turn.

Fallback if deck-search UI is unstable:

- Draw 3 cards and make them free this turn. Keep the service boundary so true search can replace it later.

## Phase 5: Reward Omen Patches

Reward modifiers should be data-driven:

| Destiny | Card/relic rarity weight | Upgraded card chance | Potion chance |
|---:|---:|---:|---:|
| 0 | 0.55x rare, 0.75x uncommon | 0.50x | 0.45x |
| 1 | 0.70x rare, 0.85x uncommon | 0.65x | 0.60x |
| 2 | 0.90x rare, 0.95x uncommon | 0.85x | 0.85x |
| 3 | 1.00x | 1.00x | 1.00x |
| 4 | 1.20x rare, 1.10x uncommon | 1.20x | 1.25x |
| 5 | 1.50x rare, 1.25x uncommon | 1.50x | 1.50x |

Rules:

- Apply only for Diviner runs.
- Change weights, not reward counts.
- Never force a rare reward if the base game would not offer a card/relic reward.
- Log before/after rarity decisions while debug config is enabled.

## Phase 6: Divination Providers

Provider contract:

- `CanResolve(runState)`
- `WouldRepeatLastResult(runState)`
- `Resolve(runState)`
- `Category`
- `CooldownWeightPenalty`

Initial provider order:

1. Boss forecasts from act/map state.
2. Elite route/node sequence if current act map exposes it.
3. Ancient identity if stored on the act model.
4. Event forecasts only when the event is already committed/generated.
5. Ancient reward options only after reward-generation internals are identified.
6. Relic queues last, and only through a no-mutation preview path.

Category cooldown:

- After any category resolves, drastically reduce that category for the next two divinations.
- If a provider would repeat because no forecasted item has been consumed yet, reject it and roll another provider.
- If all other providers fail, allow a repeat only for providers proven not to mutate live run state.

No-mutation rule:

- Forecast providers must never call APIs that pull from live reward queues, advance RNG, or create reward objects unless they operate on a deep clone. This is especially important for `RelicGrabBag`, potion odds, event selection, and reward generation.

## Phase 7: Content Expansion

Implementation order:

1. Add Basic and Common cards as simple, testable effects.
2. Refactor any early Common cards that change Destiny directly unless they are Balance or a Dredge survival tool.
3. Add Uncommon cards that do not need new patch surfaces, prioritizing Foretell, search, generated Fortune/Misfortune control, and divination payoffs over Destiny nudges.
4. Add Dredge build cards: Countdown support, Escape from Destiny support, Misfortune conversion, and "survive the clock" payoffs.
5. Add Enlightenment build cards: Fated setup, draw/search control, and peak-Destiny payoffs.
6. Add destiny/reward/divination payoff Powers.
7. Add Rare cards after the runtime has enough hooks to support large Destiny pivots.
8. Add relics in batches of 3-4 with smoke tests.
9. Add potions last, reusing stable card/relic commands.

Design guardrail:

- Keep Fortune and Misfortune as generated cards for v0. Do not also turn them into stack counters unless the card pool is rebalanced around that larger economy.
- Divination should be valuable enough that "Divinate. Exhaust." cards feel worth picking; prefer fewer, better Divination cards over many low-impact cantrips.
- Use `Foretell:` for delayed start-of-next-turn card text.
- Use `Dredge` instead of "when Destiny is 0" and `Enlightenment` instead of "when Destiny is 5" in player-facing card text.
- Define `Fated` as a temporary zero-cost marker that lasts until played or end of turn; do not use it for generic "chosen by fate" flavor text.

## Phase 7.5: Art Polish

After each card or sprite placeholder is functionally wired, replace it with polished art before marking the content batch complete.

Art direction:

- Use as few visual elements as possible.
- Use as few color schemes as possible.
- Make the primary shape readable when minimized in hand, reward screens, relic bars, and map markers.
- Prefer one iconic omen object over scenic compositions.
- Check each finished asset at its in-game display size before accepting it.

## Phase 8: QA

Core smoke tests:

- New run starts with destiny 3 and Crystal Ball.
- Destiny 0 combat can be survived by Escape cards and defeats at countdown 0.
- Enlightenment combat searches/draws 3 cards and makes them free.
- Balance works in Good Omen and Bad Omen.
- Foretell cards resolve once at next turn start, including Omen of Woes.
- Crystal Ball description records divinations and persists across save/load.
- Reward odds patch is inactive for non-Diviner characters.
- Build and Release deploy scripts pass.
