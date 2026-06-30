using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;

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
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner failed to clear runtime after {reason}: {ex}");
        }
    }
}
