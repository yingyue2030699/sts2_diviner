using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Diviner.DivinerCode.Patches;

internal static class DestinyLuckPatches
{
    [HarmonyPatch(typeof(CardRarityOdds), nameof(CardRarityOdds.Roll), [typeof(CardRarityOddsType)])]
    private static class CardRarityRollPatch
    {
        private static void Postfix(CardRarityOddsType type, ref CardRarity __result)
        {
            if (type is not (CardRarityOddsType.RegularEncounter or
                    CardRarityOddsType.EliteEncounter or
                    CardRarityOddsType.BossEncounter) ||
                TryGetDivinerPlayer(null) is not { } player)
            {
                return;
            }

            DestinyService.EnsureLoadedForRun(player.RunState);
            __result = DestinyRewardTuning.AdjustCardRarity(
                __result,
                DestinyService.CurrentDestiny,
                chance => RollChance(player, chance),
                DivinerRelicHooks.SuppressesPositiveRewardRarity(player)
            );
        }
    }

    [HarmonyPatch(typeof(PotionRewardOdds), nameof(PotionRewardOdds.Roll))]
    private static class PotionRewardRollPatch
    {
        private static void Postfix(Player player, ref bool __result)
        {
            if (!DivinerPlayerDetection.IsDivinerPlayer(player))
            {
                return;
            }

            DestinyService.EnsureLoadedForRun(player.RunState);
            __result = DestinyRewardTuning.AdjustPotionRoll(
                __result,
                DestinyService.CurrentDestiny,
                chance => RollChance(player, chance),
                player
            );
        }
    }

    [HarmonyPatch(typeof(RelicFactory), nameof(RelicFactory.RollRarity), [typeof(Player)])]
    private static class RelicRarityRollPatch
    {
        private static void Postfix(Player player, ref RelicRarity __result)
        {
            if (!DivinerPlayerDetection.IsDivinerPlayer(player))
            {
                return;
            }

            DestinyService.EnsureLoadedForRun(player.RunState);
            __result = DestinyRewardTuning.AdjustRelicRarity(
                __result,
                DestinyService.CurrentDestiny,
                chance => RollChance(player, chance),
                DivinerRelicHooks.SuppressesPositiveRewardRarity(player)
            );
        }
    }

    [HarmonyPatch(typeof(UnknownMapPointOdds), nameof(UnknownMapPointOdds.Roll))]
    private static class UnknownMapPointRollPatch
    {
        private static void Postfix(IEnumerable<RoomType> blacklist, IRunState runState, ref RoomType __result)
        {
            var player = TryGetDivinerPlayer(runState);
            if (player == null)
            {
                return;
            }

            DestinyService.EnsureLoadedForRun(runState);
            __result = DestinyRewardTuning.AdjustUnknownRoomType(
                __result,
                DestinyService.CurrentDestiny,
                blacklist.ToHashSet(),
                chance => RollChance(player, chance)
            );
        }
    }

    private static Player? TryGetDivinerPlayer(IRunState? runState)
    {
        if (runState is RunState concreteRun)
        {
            return concreteRun.Players.FirstOrDefault(DivinerPlayerDetection.IsDivinerPlayer);
        }

        var observed = DivinerCombatRuntime.GetLastObservedPlayer();
        return observed != null && DivinerPlayerDetection.IsDivinerPlayer(observed)
            ? observed
            : null;
    }

    private static bool RollChance(Player player, float chance)
    {
        if (chance <= 0f)
        {
            return false;
        }

        if (chance >= 1f)
        {
            return true;
        }

        return player.PlayerRng.Rewards.NextFloat(1f) < chance;
    }
}
