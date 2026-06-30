using Diviner.DivinerCode.Relics;
using Diviner.DivinerCode.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace Diviner.DivinerCode.Patches;

[HarmonyPatch(typeof(NClickableControl))]
internal static class CrystalBallHudTogglePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NClickableControl._GuiInput), new[] { typeof(InputEvent) })]
    private static void AfterGuiInput(NClickableControl __instance, InputEvent __0)
    {
        if (__instance is not NRelicInventoryHolder holder ||
            __0 is not InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false } ||
            holder.Relic.Model is not CrystalBall)
        {
            return;
        }

        DestinyCombatHud.Toggle();
        __instance.AcceptEvent();
    }
}
