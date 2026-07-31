using Diviner.DivinerCode.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Diviner.DivinerCode.Patches;

internal static class AncientRelicUpgradePatches
{
    [HarmonyPatch(typeof(TouchOfOrobas), "GetUpgradedStarterRelic")]
    private static class TouchOfOrobasUpgradePatch
    {
        private static bool Prefix(RelicModel starterRelic, ref RelicModel __result)
        {
            if (starterRelic is not CrystalBall)
            {
                return true;
            }

            __result = ModelDb.Relic<DestinedCrystalBall>();
            return false;
        }
    }
}
