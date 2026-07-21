using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;

namespace Diviner.DivinerCode.Patches;

[HarmonyPatch(typeof(CombatManager))]
internal static class CombatRuntimePatches
{
    [HarmonyPostfix]
    [HarmonyPatch("SetUpCombat", new[] { typeof(CombatState) })]
    private static void AfterSetUpCombat(object[] __args)
    {
        try
        {
            if (__args.Length > 0 && __args[0] is CombatState combatState)
            {
                DivinerCombatRuntime.TrackCombatState(combatState);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner failed to track combat setup: {ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("EndCombatInternal")]
    private static void AfterEndCombatInternal()
    {
        ClearRuntime("combat end");
    }

    [HarmonyPrefix]
    [HarmonyPatch("EndCombatInternal")]
    private static void BeforeEndCombatInternal()
    {
        try
        {
            var combatState = DivinerCombatRuntime.CombatState;
            if (combatState == null)
            {
                MainFile.Logger.Info("Diviner combat cleanup begin: no tracked combat state.");
                return;
            }

            var allCardsField = AccessTools.Field(typeof(CombatState), "_allCards");
            var allCards = allCardsField?.GetValue(combatState) as IEnumerable<CardModel>;
            int totalCards = allCards?.Count() ?? -1;
            int pilelessCards = allCards?.Count(card => card.Pile == null) ?? -1;
            MainFile.Logger.Info(
                $"Diviner combat cleanup begin: players={combatState.Players.Count}, totalCards={totalCards}, pilelessCards={pilelessCards}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner combat cleanup diagnostics failed: {ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("Reset", new[] { typeof(bool) })]
    private static void AfterReset()
    {
        ClearRuntime("combat manager reset");
    }

    private static void ClearRuntime(string reason)
    {
        try
        {
            DivinerCombatRuntime.ClearCombatState();
            DestinyCombatHud.CloseAndDispose();
            MainFile.Logger.Info($"Diviner runtime cleared after {reason}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner failed to clear runtime after {reason}: {ex}");
        }
    }
}
