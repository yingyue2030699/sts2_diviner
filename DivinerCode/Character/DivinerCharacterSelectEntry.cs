using BaseLib.Abstracts;
using Diviner.DivinerCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace Diviner.DivinerCode.Character;

public class DivinerCharacterSelectEntry : CustomCharacterSelectEntry
{
    public override string EntryId => "Diviner-CharacterSelect";

    public override string ButtonIconPath => "char_select_diviner.png".CharacterUiPath();

    public override string EntryTitle => "The Diviner";

    public override string EntryDescription =>
        "A seer who bargains with fortune, misfortune, and the shape of tomorrow.";

    public override int SortOrder => 40;

    public override bool UnlockedInCharacterSelect => true;

    public override CharacterModel? InitialCharacter =>
        ModelDb.AllCharacters.FirstOrDefault(character => character is Diviner);

    public override bool ShowVanillaInfoPanelWhenUnresolved => true;

    public override bool ShowVanillaInfoPanelWhenResolved => true;

    public override Control CreateCharacterSelectScene()
    {
        var root = new Control
        {
            Name = "DivinerCharacterSelectScene",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var portrait = new TextureRect
        {
            Name = "DivinerCharacterSelectPortrait",
            Texture = ResourceLoader.Load<Texture2D>("diviner_character_select.png".CharacterImagePath()),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        portrait.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        portrait.OffsetLeft = 240f;
        portrait.OffsetTop = 40f;
        portrait.OffsetRight = -240f;
        portrait.OffsetBottom = -40f;

        root.AddChild(portrait);
        return root;
    }

    public override void RegisterScene(Control sceneRoot, CustomCharacterSelectContext context)
    {
        var character = InitialCharacter;
        if (character != null)
        {
            context.SetCharacter(character);
        }
    }
}
