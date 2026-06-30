using BaseLib.Abstracts;
using Diviner.DivinerCode.Cards;
using Diviner.DivinerCode.Extensions;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
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

    public override List<(string, string)>? Localization => new CharacterLoc(
        "The Diviner",
        "The Diviner",
        "A seer who bargains with fortune, misfortune, and the shape of tomorrow.",
        "them",
        "they",
        "theirs",
        "their",
        "Destiny",
        "The omen is clear.",
        "The thread is cut.",
        "The Diviner reads the line between victory and ruin.",
        "Every future has a price.",
        "Diviner Cards",
        "Destiny, divinations, delayed woes, and generated fortunes."
    );

    public override Color NameColor => Color;

    public override CharacterGender Gender => CharacterGender.Neutral;

    public override int StartingHp => 72;

    public override int BaseOrbSlotCount => 0;

    public override string CustomIconTexturePath => "character_icon_diviner.png".ImagePath();

    public override string CustomMapMarkerPath => "map_marker_diviner.png".ImagePath();

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
