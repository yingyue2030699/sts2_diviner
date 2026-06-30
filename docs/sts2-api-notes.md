# STS2 API Notes

These notes come from the installed `sts2.xml`, BaseLib `3.3.2` XML docs, and local ThePerfect examples.

## Combat Hooks

- `AbstractModel.AfterRoomEntered(AbstractRoom)` can detect `CombatRoom`, but game docs warn that start-of-combat effects like drawing or gaining block should usually use side-turn hooks with a round check.
- `AbstractModel.BeforeSideTurnStart(PlayerChoiceContext, CombatSide, IReadOnlyList<Creature>, ICombatState)` is suitable for player start-of-turn commands.
- `AbstractModel.AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)` is suitable when no `PlayerChoiceContext` is needed.
- `AbstractModel.AfterPlayerTurnStart(PlayerChoiceContext, Player)` is another option when the effect needs a player choice after the turn has started.
- `AbstractModel.BeforeSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)` is the correct hook for Countdown of Destiny. Gate by player side and participant membership before ticking.
- ThePerfect's `FlawlessComputingUnit` uses `BeforeSideTurnStart` plus a per-combat one-shot guard.
- Use `CreatureCmd.Damage(...)` for explicit damage actions and `CreatureCmd.Kill(creature, true)` for unavoidable Dredge defeat.

## Generated Cards

- `RunState.CreateCard<T>(Player)`, `RunState.CreateCard(CardModel, Player)`, `CombatState.CreateCard<T>(Player)`, and `CombatState.CreateCard(CardModel, Player)` create card instances.
- `CardPileCmd.AddGeneratedCardToCombat(CardModel, PileType, Player, CardPilePosition)` adds generated cards.
- `CardPileCmd.AddGeneratedCardsToCombat(IEnumerable<CardModel>, PileType, Player, CardPilePosition)` handles batches.
- `CardPileCmd.Add(...)` moves existing combat cards between piles.
- `CardPileCmd.Draw(PlayerChoiceContext, decimal, Player, bool)` draws cards.
- Current Dredge v0 uses `AddGeneratedCardsToCombat(..., PileType.Draw, ..., CardPilePosition.Bottom)`. A true "shuffle into draw pile" command still needs a focused API spike or a safe draw-pile shuffle command.

## Card Search

- Read the draw pile with `PileType.Draw.GetPile(player).Cards` or `player.PlayerCombatState.DrawPile.Cards`.
- `CardSelectCmd.FromSimpleGrid(...)` supports Enlightenment-style searches.
- Move chosen cards to hand with `CardPileCmd.Add(selectedCards, PileType.Hand, CardPilePosition.Bottom, source, false)`.

## Cost Modification

- `CardEnergyCost.SetUntilPlayed`, `SetThisTurnOrUntilPlayed`, `SetThisTurn`, and `SetThisCombat` apply absolute local costs.
- `CardEnergyCost.AddUntilPlayed`, `AddThisTurnOrUntilPlayed`, `AddThisTurn`, and `AddThisCombat` apply relative local costs.
- Use `SetThisTurnOrUntilPlayed(0, false)` for Fated/free-this-turn cards unless a different lifetime is required.
- Escape from Destiny currently grows through a shared combat tax in `TryModifyEnergyCostInCombat`. Per-card `AddThisCombat(1, false)` remains a viable alternative if we decide each copy should track its own cost.
- Add a keyword only on upgrade with `WithKeyword(CardKeyword.Retain, UpgradeType.Add)`. Remove one on upgrade with `WithKeyword(keyword, UpgradeType.Remove)` or an explicit `RemoveKeyword(...)` in `OnUpgrade`.

## Damage and HP-Loss Hooks

- `AbstractModel.AfterDamageReceived(PlayerChoiceContext, Creature, DamageResult, ValueProp, Creature?, CardModel?)` can drive "whenever you lose HP" effects because it includes a `PlayerChoiceContext`.
- Gate HP-loss effects on the owner creature and `DamageResult.UnblockedDamage > 0`.
- `AfterCurrentHpChanged(Creature, decimal)` is broader but does not provide `PlayerChoiceContext`, so it is poor for effects that draw or open choices.

## Reward Hooks

- `ModifyCardRewardCreationOptions(Player, CardCreationOptions)` can alter card reward creation options before generation.
- `TryModifyCardRewardOptions(Player, List<CardCreationResult>, CardCreationOptions)` can alter generated card reward options.
- `ModifyCardRewardUpgradeOdds(Player, CardModel, decimal)` directly adjusts upgrade odds.
- `ShouldForcePotionReward(Player, RoomType)` and `ShouldProcurePotion(PotionModel, Player)` support potion force/veto behavior.
- `PotionRewardOdds.Roll(...)` mutates future potion odds; do not call it for previews.
- `CardCreationOptions.RarityOdds` is a `CardRarityOddsType` preset (`None`, `RegularEncounter`, `EliteEncounter`, `BossEncounter`, `Shop`, `Uniform`), with a public getter and non-public setter. Rebuild options with the public constructors when changing presets; arbitrary scalar multipliers still need a deeper patch.
- Arbitrary potion percentage tuning still needs investigation around a no-mutation `PotionRewardOdds` path.
- Relic rarity does not currently have a clean public hook. Avoid `RelicGrabBag` pull APIs until a snapshot path is proven.

## Forecasting

- Boss forecast leads: `IRunState.Act`, `ActModel.BossEncounter`, `ActModel.SecondBossEncounter`, `ActModel.HasSecondBoss`, `IRunState.Map`, `ActMap.BossMapPoint`, and `ActMap.SecondBossMapPoint`.
- Ancient forecast lead: `ActModel.Ancient`. Do not call `PullAncient()` for previews.
- Map/elite forecast leads: `ActMap.GetAllMapPoints()`, `ActMap.GetPointsInRow(...)`, `MapPoint.PointType`, `MapPoint.Children`, `MapPoint.Quests`, `IRunState.CurrentMapPoint`, and path history.
- Event forecast lead: `EventRoom.CanonicalEvent`, but future unknown event selection may be lazy. Do not call `ActModel.PullNextEvent(...)`.
- Relic queue forecast can snapshot IDs with `RelicGrabBag.ToSerializable().RelicIdLists`. Do not call pull or roll APIs (`PullNextEvent`, `PullNextEncounter`, `RollRarity`, `RewardsSet.GenerateRewardsFor`) from divination providers unless operating on a deep clone.

## UI

- Safe first path: standalone `CanvasLayer`, mirroring ThePerfect's `PerfectCombatHud`.
- Native top-bar path: `NGlobalUi`, `NTopBar`, and related `MegaCrit.sts2.Core.Nodes.TopBar` nodes. Treat as fragile until combat mechanics are stable.

## Persistence

- BaseLib `SavedSpireField<TTarget,TValue>` can use `RegisterCustomSave()` for `RunState` and `Player` even though those are not base-game-supported `SavedProperty` targets.
- STS2 `SavedPropertyAttribute` supports ints, bools, strings, model IDs, int arrays, card references, and card arrays on supported model targets.
- `SavedSpireField<RunState,int>` is used for Destiny. Divination records use `SavedSpireField<RunState,string>` with JSON encoding because list-like values need custom serializer verification.
- Compile-time persistence wiring is complete; manual save/load smoke testing is still required.
