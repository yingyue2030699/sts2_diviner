# Diviner Plan Status

Last updated: 2026-06-30

## Finalized for v0 Implementation

| Area | Status |
|---|---|
| Development environment | Scaffold, BaseLib dependency, quick PCK packing, Steam copy targets, build script, and deploy script are in place. |
| Character foundation | Diviner character model, starter deck, Crystal Ball starter relic, card/relic/potion pools, localization, and placeholder resources are wired. |
| Vocabulary cleanup | Player-facing text now uses `Foretell`, `Doomed`, `Revelation`, and a concrete `Fated` definition. Internal identifiers still use the old Dredge/Enlightenment names for compatibility. |
| Starter mechanics | Balance, Omen of Woes, Fortune, Misfortune, and Crystal Ball compile and are wired into the starter kit. |
| Doomed v0 | The first Destiny 0 each combat applies Countdown of Destiny 3, generates 3 Escape from Destiny cards in both draw and discard piles, ticks countdown at player turn end, and defeats the player at 0. |
| Revelation v0 | Destiny 5 opens a draw-pile search, moves up to 3 chosen cards to hand, and makes them Fated with absolute cost 0 for the turn. |
| Destiny scale v1 | Destiny now uses tiered effects: 3+ prevents unknown-room combats, 4+ halves common card/relic outcomes, 5 adds Revelation, 2- guarantees unknown-room combat when allowed, 1- increases common card/relic outcomes, and 0 adds Doomed. |
| Card balance update | `docs/card-pool-v0.md` is reconciled with `docs/card-pool-v01-balance-fix.md`, including Regent-collision renames. |
| Common card batch | Implemented commons now include Palm Strike, Destiny's Fall, Crossed Lines, Thread Cut, Wax Seal, Misread Strike, Eclipse Jab, Forewarned Blow, Destined Lance, Read the Room, Insurance, Bad Feeling, Palm Reading, Lucky Break, Omen of Shelter, False Alarm, Omens Align, Skeptic's Charm, Reconsult, Narrow Escape, Thread Pull, Ward Sign, Smoke and Mirrors, and Small Ritual. |
| Uncommon and rare card batch | All cards listed in `docs/card-pool-v01-balance-fix.md` now have playable implementations and in-code English/Simplified Chinese localization. |
| Persistence v0 | Destiny is stored with `SavedSpireField<RunState,int>`; divination records are stored as JSON through `SavedSpireField<RunState,string>`. |
| Art batch v3 | Every implemented Diviner card now has regular and big portrait PNGs. The latest pass removes the remaining exact duplicate portraits with distinct minimalist symbols. |
| Card description polish | Upgraded Diviner cards now route to `upgradedDesc` localization when present, keyword terms are colored, and dynamic value markers resolve from live card variables before display. |

## Confirmed Deferred Gates

| Area | Reason |
|---|---|
| Saved Destiny and divination records | Compile-time persistence wiring is complete. Manual in-game save/load smoke testing is still needed before this is considered runtime-proven. |
| Full divination providers | Boss, ancient, event, and single-relic queue read paths are implemented through `DivinationService`; elite divination is skipped because map nodes do not expose committed elite encounters. Runtime verification still needs in-game smoke tests. |
| Divination activity tracking | Divination records now carry active/inactive state, the HUD has a default-on `hide inactive` checkbox, and relic divinations become inactive when their foretold relic leaves the queue. Boss/event/act rollover activity should be smoke-tested in game. |
| True Dredge shuffle | Generated Escape cards are added to the draw pile in v0. A true shuffle command/path needs API confirmation. |
| Generic Foretell engine | Current Foretell cards use per-card next-turn queues plus shared UI/status tracking and Ledger of Signs counting. A single generic effect queue can wait until runtime behavior is proven. |
| Fatal riders | Line of Fate and The Last Word now use post-attack alive checks. Runtime smoke testing should confirm this catches all fatal cases. |
| In-game smoke tests | Build-time verification passes, but selectability, save/load, start-of-combat sequencing, and reward screens still need manual in-game confirmation. |

## Next Implementation Batch

1. In-game smoke test Destiny/divination persistence across save/load, combat start, reward screens, and act transitions.
2. Runtime-test boss, ancient, event, and single-relic divination records across act transitions and reward pickups.
3. Playtest the tiered Destiny reward/unknown-room scale; tune the 50% common-rarity shift only if it feels too blunt.
4. Add a debug/dev-only Destiny setter card or console command to speed Dredge/Enlightenment smoke tests.
5. Runtime-test upgraded-card descriptions, especially cards with `upgradedDesc`, and inspect newly generated uncommon/rare card art at both hand and zoom sizes.
