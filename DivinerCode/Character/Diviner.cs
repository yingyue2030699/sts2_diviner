using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Diviner.DivinerCode.Cards;
using Diviner.DivinerCode.Extensions;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace Diviner.DivinerCode.Character;

public class Diviner : PlaceholderCharacterModel
{
    public const string CharacterId = "Diviner";

    public static readonly Color Color = new("c8b8ff");

    public override string PlaceholderID => "defect";

    public override bool HideFromVanillaCharacterSelect => true;

    public override List<(string, string)>? Localization => new CharacterLoc(
        DivinerLoc.Text("The Diviner", "占卜师"),
        DivinerLoc.Text("The Diviner", "占卜师"),
        DivinerLoc.Text(
            "A seer who bargains with fortune, misfortune, and the shape of tomorrow.",
            "与福运、厄运，以及明日轮廓交易的预言者。"),
        DivinerLoc.Text("them", "其"),
        DivinerLoc.Text("they", "其"),
        DivinerLoc.Text("theirs", "其"),
        DivinerLoc.Text("their", "其"),
        DivinerLoc.Text("Destiny", "命运"),
        DivinerLoc.Text("The omen is clear.", "预兆已明。"),
        DivinerLoc.Text("The thread is cut.", "命线已断。"),
        DivinerLoc.Text(
            "The Diviner reads the line between victory and ruin.",
            "占卜师读出胜利与毁灭之间的细线。"),
        DivinerLoc.Text("Every future has a price.", "每一种未来都有代价。"),
        DivinerLoc.Text("Diviner Cards", "占卜师牌"),
        DivinerLoc.Text(
            "Destiny, divinations, delayed woes, and generated fortunes.",
            "命运、占卜、延迟的灾厄与生成的福运。")
    );

    public override Color NameColor => Color;

    public override CharacterGender Gender => CharacterGender.Neutral;

    public override int StartingHp => 72;

    public override int BaseOrbSlotCount => 0;

    public override string CustomCharacterSelectIconPath => "char_select_diviner.png".CharacterUiPath();

    public override string CustomCharacterSelectLockedIconPath => "char_select_diviner_locked.png".CharacterUiPath();

    public override Control CustomIcon => new TextureRect
    {
        CustomMinimumSize = new Vector2(72f, 72f),
        Texture = ResourceLoader.Load<Texture2D>("character_icon_diviner.png".ImagePath()),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
    };

    public override string CustomIconTexturePath => "character_icon_diviner.png".ImagePath();

    public override string CustomIconOutlineTexturePath => "character_icon_diviner.png".ImagePath();

    public override string CustomMapMarkerPath => "map_marker_diviner.png".ImagePath();

    public override string CustomMerchantAnimPath => "diviner_merchant.png".CharacterImagePath();

    public override string CustomRestSiteAnimPath => "diviner_rest_site.png".CharacterImagePath();

    public override NCreatureVisuals CreateCustomVisuals()
    {
        var visuals = NodeFactory<NCreatureVisuals>.CreateFromResource(
            ResourceLoader.Load<Texture2D>("diviner_combat_idle.png".CharacterImagePath()));
        visuals.Scale = new Vector2(0.75f, 0.75f);
        return visuals;
    }

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeDiviner>(),
        ModelDb.Card<StrikeDiviner>(),
        ModelDb.Card<StrikeDiviner>(),
        ModelDb.Card<StrikeDiviner>(),
        ModelDb.Card<DefendDiviner>(),
        ModelDb.Card<DefendDiviner>(),
        ModelDb.Card<DefendDiviner>(),
        ModelDb.Card<DefendDiviner>(),
        ModelDb.Card<Balance>(),
        ModelDb.Card<DivinationOfWoes>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<CrystalBall>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<DivinerCardPool>();

    public override RelicPoolModel RelicPool => ModelDb.RelicPool<DivinerRelicPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<DivinerPotionPool>();

    public override Task AfterCombatVictory(CombatRoom room)
    {
        var player = room.CombatState.Players.FirstOrDefault(DivinerPlayerDetection.IsDivinerPlayer);
        if (player != null)
        {
            DestinyService.EnsureLoadedForRun(player.RunState);
            DestinyService.RecordCombatEndLuck(player.RunState);
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyCardRewardUpgradeOdds(Player player, CardModel card, decimal odds)
    {
        DestinyService.EnsureLoadedForRun(player.RunState);
        return Math.Clamp(
            odds * DestinyRewardTuning.UpgradeOddsMultiplier(DestinyService.CurrentDestiny),
            0m,
            1m
        );
    }
}
