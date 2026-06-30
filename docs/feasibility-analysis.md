# Diviner Feasibility Analysis

## Concept Summary

Diviner is a knowledge-and-fate character built around `destiny`, a persistent 0-5 run value. Values 0-2 are Bad Omen, values 3-5 are Good Omen. Destiny changes combat tempo, post-combat reward odds, and the reliability of Diviner cards.

The second pillar is `divinate`: reveal and record future run information in Crystal Ball. Divination is intended to be strategic information rather than direct combat power, with some cards and relics paying off for recorded prophecies.

## Sources Consulted

- BaseLib Wiki: custom models, custom card pools, custom keywords, AddedNode UI hooks, and saved property support.
- Local reference mod: `/Users/yueying/Documents/Playground/ThePerfectCode`.
- Local STS2 install and BaseLib package/deployed mod manifests.
- D&D Beyond Wizard page for the wizard/diviner fantasy of specialized knowledge and future glimpses.
- General divination, fortune-telling, omen, and prophecy references for theme vocabulary.

## Feasibility Matrix

| Mechanic | Feasibility | Notes | First implementation route |
|---|---:|---|---|
| Custom character, card pool, relic pool, potion pool | High | ThePerfect already uses this successfully through BaseLib custom models. | Add `Diviner` character model, pool models, base card/relic/potion classes, localization, and starter deck/relic. |
| Persistent `destiny` 0-5 | High | BaseLib notes `[SavedProperty]` support and SpireField-style extensions. Runtime services can clamp and broadcast changes. | `DestinyService` plus saved run/player field, default 3 on new Diviner run. |
| Combat `destiny` UI | High | BaseLib supports AddedNode; ThePerfect has a manual CanvasLayer HUD pattern. | Start with combat HUD overlay, then move to top bar once target scene is identified. |
| Non-combat top bar display | Medium | Needs scene identification for map/reward/shop screens. Likely Harmony or AddedNode per scene. | Phase 2 after combat HUD proves state model. |
| Good/Bad Omen card text | High | Cards can read current destiny and branch in `OnPlay`. | Add helper predicates `IsGoodOmen`, `IsBadOmen`, `RequireBadOmenTax`. |
| Balance starter card | High | Conditional cost/tax is easy if tax is enforced on play; dynamic preview may need later UI polish. | First version: if Bad Omen and energy < base+2, fail play or use dynamic cost hook. |
| Divination of Woes delayed effect | High | Similar to powers/statuses that trigger at next turn start. | Temporary power/status with stored damage/debuff amount. |
| Fortune/Misfortune generated cards | High | Generated custom cards can be added to hand; Misfortune autoplay can be implemented through end-turn relic/power hook. | Crystal Ball adds one generated card at combat start; end-turn hook plays Misfortune. |
| Dredge at Destiny 0 | Medium | Start-of-combat hook and countdown power are straightforward. Defeat command and cost-scaling Escape cards need API confirmation. | Implement countdown as a power; Escape increments countdown and a combat escape-tax field. |
| Enlightenment at Destiny 5 | Medium | Searching deck and temporary free cards should be possible, but player-choice UI and cost modification need specific API selection. | Start with a simple deck-selection command; fallback to "draw 3, make free" if choice UI is unstable. |
| Card reward rarity modifier | Medium | Likely Harmony patch over reward generation. Needs class names and mod compatibility care. | Patch only Diviner runs; change weights, not direct replacement. |
| Upgraded card chance in acts 2/3 | Medium | Same as reward generation. Need find upgrade-roll location. | Apply deterministic multiplier to existing chance. |
| Potion chance after combat | Medium | Forcing or vetoing potions is explicit; arbitrary percentage tuning needs the exact odds hook. | Start with force/veto at destiny extremes, then inspect `PotionRewardOdds.Roll`. |
| Relic reward rarity modifier | Medium/High risk | Relic factory pulls mutate `RelicGrabBag`; replacing reward relics can desync queues. | Leave until late; prefer modifying selection options before any relic is pulled. |
| Relic queue divination | High risk | The desired queue data exists, but previewing private deques or pull methods can mutate state. | Only implement with reflection/deep clone after tests prove no live state changes. |
| Boss divination | High | Act model/map state expose boss and second-boss information. | Implement early through `ActModel.BossEncounter`, `SecondBossEncounter`, and map state. |
| Ancient reward divination | Uncertain | The act's Ancient appears readable; exact reward options may still be generated lazily. | Start by forecasting the Ancient, then investigate reward options separately. |
| Next event divination | Medium/High risk | Current/generated event state is readable. Future unknown event may require RNG/filter simulation. | Prefer "next committed event" first; avoid RNG simulation in MVP. |
| Elite sequence divination | Medium | Map elite nodes and route sequence are likely available; exact encounter models may be generated on entry. | Forecast elite nodes/sequence first, exact elite identity later. |
| Dynamic Crystal Ball descriptor | Medium | BaseLib mentions description overrides; relic descriptions may need refresh hooks. | Store records separately first; then add description override and UI refresh. |

## Feasibility Conclusions

The core character is feasible: destiny, omen-branching cards, generated cards, starter relic, basic divination counters, and a combat HUD are all within established BaseLib/local mod patterns.

The highest-risk area is not combat. It is exact future-run prediction. Relics are the best first target because the concept already assumes queues. Boss, ancient reward, event, and elite predictions should be implemented behind small forecast providers so unsupported categories can gracefully decline and re-roll to another category.

Reward manipulation is feasible but should be conservative. It touches global generation systems, so every patch must be character-gated, deterministic, and easy to disable during debugging.

## Recommended MVP

1. Diviner character shell with starter deck and Crystal Ball.
2. Destiny state, combat HUD, and Good/Bad Omen helpers.
3. Crystal Ball adds Fortune or Misfortune at combat start.
4. Balance and Divination of Woes.
5. Dredge and Enlightenment combat-start effects.
6. Divinate records placeholder entries plus safe boss/map forecasts.
7. Card reward and upgraded-card modifiers.
8. Potion force/veto behavior.
9. Expand event/ancient/relic forecasts only after no-mutation tests exist.

## API Leads

- Combat/reward hooks: `AbstractModel`, `AfterRoomEntered`, `BeforeSideTurnStart`, `AfterSideTurnStart`, `ModifyCardRewardCreationOptions`, `TryModifyCardRewardOptions`, `ModifyCardRewardUpgradeOdds`, `ShouldForcePotionReward`, `ShouldProcurePotion`.
- Reward types: `CardCreationOptions`, `CardCreationFlags`, `CardRarityOddsType`, `Reward`, `CardReward`, `RelicReward`, `SpecialCardReward`, `RewardsSet`.
- Forecast data: `IRunState`, `ActModel`, `ActMap`, `MapPoint`, `MapTravel`, `EventRoom`, `CombatRoom`.
- Risky forecast data: `RelicFactory`, `RelicGrabBag`, `PotionRewardOdds`.
- UI: `NGlobalUi`, `NTopBar`, plus the safer custom `CanvasLayer` pattern from ThePerfect.
- Costs/cards: `CardEnergyCost`, `LocalCostModifier`, `LocalCostModifierExpiration`, `CardPileCmd.AddGeneratedCardToCombat`.
- Persistence: `SavedPropertyAttribute`, game saved properties, and BaseLib saved-field helpers. Exact BaseLib 3.3.2 syntax still needs inspection before implementation.
