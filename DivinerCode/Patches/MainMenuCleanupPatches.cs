using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace Diviner.DivinerCode.Patches;

[HarmonyPatch(typeof(NMainMenu))]
internal static class MainMenuCleanupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    private static void AfterReady()
    {
        DivinerCombatRuntime.ClearRunState();
        DestinyCombatHud.CloseAndDispose();
    }
}
