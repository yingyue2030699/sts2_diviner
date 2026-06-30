using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace Diviner.DivinerCode.Patches;

[HarmonyPatch(typeof(RunManager))]
internal static class RunCleanupPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(RunManager.CleanUp), new[] { typeof(bool) })]
    private static void AfterCleanUp()
    {
        DivinerCombatRuntime.ClearRunState();
        DestinyCombatHud.CloseAndDispose();
    }
}
