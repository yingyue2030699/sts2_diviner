using Diviner.DivinerCode.Character;
using Diviner.DivinerCode.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Diviner.DivinerCode.Patches;

[HarmonyPatch(typeof(RestSiteSynchronizer), nameof(RestSiteSynchronizer.BeginRestSite))]
internal static class RestSiteGenerationDiagnosticsPatch
{
    [HarmonyPrefix]
    private static void BeforeBeginRestSite()
    {
        var player = DivinerCombatRuntime.GetLastObservedPlayer();
        MainFile.Logger.Info(
            $"Diviner rest-site generation begin: trackedPlayer={player?.NetId.ToString() ?? "none"}, " +
            $"isDiviner={player != null && DivinerPlayerDetection.IsDivinerPlayer(player)}, " +
            $"combatTracked={DivinerCombatRuntime.CombatState != null}.");
    }

    [HarmonyPostfix]
    private static void AfterBeginRestSite(RestSiteSynchronizer __instance)
    {
        try
        {
            var options = __instance.GetLocalOptions();
            MainFile.Logger.Info(
                $"Diviner rest-site generation complete: optionCount={options.Count}, " +
                $"options=[{string.Join(",", options.Select(option => option.OptionId))}].");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Diviner could not inspect generated rest-site options: {ex}");
        }
    }

    [HarmonyFinalizer]
    private static Exception? AfterBeginRestSiteException(Exception? __exception)
    {
        if (__exception != null)
        {
            MainFile.Logger.Error($"Diviner rest-site generation threw an exception: {__exception}");
        }

        return __exception;
    }
}

[HarmonyPatch(typeof(NRestSiteRoom), "_Ready")]
internal static class RestSiteRoomDiagnosticsPatch
{
    [HarmonyPrefix]
    private static void BeforeReady()
    {
        try
        {
            var player = DivinerCombatRuntime.GetLastObservedPlayer();
            string path = player?.Character.RestSiteAnimPath ?? "none";
            MainFile.Logger.Info(
                $"Diviner rest-site room ready begin: player={player?.NetId.ToString() ?? "none"}, " +
                $"character={player?.Character.Id.Entry ?? "none"}, resource={path}, " +
                $"resourceExists={path != "none" && ResourceLoader.Exists(path)}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner rest-site room preflight diagnostics failed: {ex}");
        }
    }

    [HarmonyPostfix]
    private static void AfterReady(NRestSiteRoom __instance)
    {
        try
        {
            MainFile.Logger.Info(
                $"Diviner rest-site room ready complete: optionCount={__instance.Options.Count}, " +
                $"characterCount={__instance.characterAnims.Count}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner rest-site room completion diagnostics failed: {ex}");
        }
    }

    [HarmonyFinalizer]
    private static Exception? AfterReadyException(Exception? __exception)
    {
        if (__exception != null)
        {
            MainFile.Logger.Error($"Diviner rest-site room setup threw an exception: {__exception}");
        }

        return __exception;
    }
}
