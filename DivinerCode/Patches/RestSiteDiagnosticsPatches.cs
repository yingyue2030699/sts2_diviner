using Diviner.DivinerCode.Character;
using Diviner.DivinerCode.Extensions;
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
    private const string StaticPortraitNodeName = "DivinerRestSitePortrait";

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
            AddStaticDivinerPortrait(__instance);
            MainFile.Logger.Info(
                $"Diviner rest-site room ready complete: optionCount={__instance.Options.Count}, " +
                $"characterCount={__instance.characterAnims.Count}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner rest-site room completion diagnostics failed: {ex}");
        }
    }

    private static void AddStaticDivinerPortrait(NRestSiteRoom room)
    {
        var bgContainer = AccessTools
            .Property(typeof(NRestSiteRoom), "BgContainer")
            ?.GetValue(room) as Control;
        var divinerCharacters = room.Characters
            .Where(character =>
                character.Player != null &&
                DivinerPlayerDetection.IsDivinerPlayer(character.Player))
            .ToList();
        if (bgContainer == null ||
            divinerCharacters.Count == 0 ||
            bgContainer.GetNodeOrNull<TextureRect>(StaticPortraitNodeName) != null)
        {
            return;
        }

        string portraitPath = "diviner_restsite.png".CharacterImagePath();
        var portraitTexture = ResourceLoader.Load<Texture2D>(portraitPath);
        if (portraitTexture == null)
        {
            MainFile.Logger.Error(
                $"Diviner static rest-site portrait could not be loaded: path={portraitPath}.");
            return;
        }

        foreach (var character in divinerCharacters)
        {
            character.Visible = false;
        }

        var portrait = new TextureRect
        {
            Name = StaticPortraitNodeName,
            Texture = portraitTexture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        bgContainer.AddChild(portrait);
        portrait.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        MainFile.Logger.Info(
            $"Diviner static rest-site portrait installed beneath UI; hiddenCharacterCount={divinerCharacters.Count}.");
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
