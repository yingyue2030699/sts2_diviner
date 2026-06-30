using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Diviner.DivinerCode.Patches;

[HarmonyPatch(typeof(NCombatUi))]
internal static class CombatHudPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    private static void AfterReady(NCombatUi __instance)
    {
        MountOrRefresh("ready");
    }

    [HarmonyPostfix]
    [HarmonyPatch("Enable")]
    private static void AfterEnable(NCombatUi __instance)
    {
        MountOrRefresh("enable");
    }

    [HarmonyPostfix]
    [HarmonyPatch("AnimIn")]
    private static void AfterAnimIn(NCombatUi __instance)
    {
        MountOrRefresh("anim in");
    }

    [HarmonyPostfix]
    [HarmonyPatch("Activate")]
    private static void AfterActivate(NCombatUi __instance, CombatState state)
    {
        DivinerCombatRuntime.TrackCombatState(state);
        MountOrRefresh("activate");
    }

    private static void MountOrRefresh(string reason)
    {
        try
        {
            DestinyCombatHud.EnsureMounted();
            DestinyCombatHud.RefreshIfMounted();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner failed to mount destiny HUD on combat UI {reason}: {ex}");
        }
    }
}
