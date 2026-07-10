using Diviner.DivinerCode.Cards;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Diviner.DivinerCode.Potions;

public class BottledOmen : DivinerPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override List<(string, string)>? Localization => DivinerLoc.Potion(
        "Bottled Omen",
        "The next card is played with Good Omen, Bad Omen, and Revelation effects regardless of Destiny.",
        "瓶装预兆",
        "下一张牌无视命运，视为同时满足吉兆、凶兆与启示效果。"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null)
        {
            return;
        }

        DivinerCombatRuntime.ForceNextCardFullOmen(player);
        await DivinerStatusPowerSync.Sync(player, choiceContext);
    }
}

public class BitterTea : DivinerPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override List<(string, string)>? Localization => DivinerLoc.Potion(
        "Bitter Tea",
        "Divinate. Draw 1 card.",
        "苦茶",
        "占卜。抽 1 张牌。"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null)
        {
            return;
        }

        await DivinationService.RecordPlaceholder(choiceContext, player, "Bitter Tea");
        await CardPileCmd.Draw(choiceContext, 1, player, false);
    }
}

public class TarOfDread : DivinerPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override List<(string, string)>? Localization => DivinerLoc.Potion(
        "Tar of Dread",
        "Add 3 Misfortune to your hand.",
        "恐惧焦油",
        "将 3 张厄运加入你的手牌。"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null)
        {
            return;
        }

        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(player, 3, PileType.Hand, CardPilePosition.Bottom);
    }
}

public class BrewOfBrew : DivinerPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.AnyTime;

    public override bool CanBeGeneratedInCombat => false;

    public override List<(string, string)>? Localization => DivinerLoc.Potion(
        "Brew of Brew",
        "Heal 5 HP and discard all other potions. Gain 1 Destiny and heal 5 extra HP for each potion discarded.",
        "酿中酿",
        "回复 5 点生命并丢弃所有其他药水。获得 1 点命运；每因此丢弃 1 瓶药水，额外回复 5 点生命。"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null)
        {
            return;
        }

        var otherPotions = player.Potions
            .Where(potion => !ReferenceEquals(potion, this))
            .ToList();
        foreach (var potion in otherPotions)
        {
            await PotionCmd.Discard(potion);
        }

        await CreatureCmd.Heal(player.Creature, 5 + otherPotions.Count * 5, true);
        DestinyService.AddDestiny(1);
        DestinyService.PersistCurrentState(player.RunState);
        await DivinerStatusPowerSync.Sync(player, choiceContext);
    }
}

public class MercuryMirror : DivinerPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override List<(string, string)>? Localization => DivinerLoc.Potion(
        "Mercury Mirror",
        "Look at 2 random cards from your draw pile. Put 1 into your hand; it is Fated this turn. Put the other on the bottom of your draw pile.",
        "水银镜",
        "查看抽牌堆中 2 张随机牌。将 1 张加入手牌；本回合它为注定。将另一张置于抽牌堆底。",
        ("selectPrompt", "Choose a card to put into your hand.", "选择 1 张加入你的手牌。")
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null)
        {
            return;
        }

        var drawCards = PileType.Draw.GetPile(player).Cards.ToList();
        if (drawCards.Count == 0)
        {
            return;
        }

        var candidates = drawCards
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(2, drawCards.Count))
            .ToList();

        var selected = candidates.Count == 1
            ? candidates
            : (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                player,
                new CardSelectorPrefs(new LocString("potions", $"{Id.Entry}.selectPrompt"), 1, 1)))
            .Take(1)
            .ToList();

        if (selected.Count == 0)
        {
            selected = candidates.Take(1).ToList();
        }

        var chosen = selected[0];
        DivinerCardActions.MakeFated(chosen);
        await CardPileCmd.Add([chosen], PileType.Hand, CardPilePosition.Bottom, this, false);

        var rejected = candidates.Where(card => !ReferenceEquals(card, chosen)).ToList();
        if (rejected.Count > 0)
        {
            await CardPileCmd.Add(rejected, PileType.Draw, CardPilePosition.Bottom, this, false);
        }
    }
}

public class CondensedMisfortune : DivinerPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override List<(string, string)>? Localization => DivinerLoc.Potion(
        "Condensed Misfortune",
        "Add a Misfortune to your hand. It costs 0 this combat.",
        "凝缩厄运",
        "将 1 张厄运加入你的手牌。本场战斗中它耗能为 0。"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null || DivinerCombatRuntime.CombatState == null)
        {
            return;
        }

        var card = DivinerCombatRuntime.CombatState.CreateCard(ModelDb.Card<Misfortune>(), player);
        card.SetToFreeThisCombat();
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Bottom);
    }
}

public class StarlessDraught : DivinerPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override List<(string, string)>? Localization => DivinerLoc.Potion(
        "Starless Draught",
        "Gain 2 Destiny. If this enters Revelation, gain 2 Energy.",
        "无星药酒",
        "获得 2 点命运。若因此进入启示，获得 2 点能量。"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null)
        {
            return;
        }

        bool wasEnlightened = DivinerCombatRuntime.HasEnlightenmentEffect(player);
        DestinyService.AddDestiny(2);
        DestinyService.PersistCurrentState(player.RunState);
        await DivinerStatusPowerSync.Sync(player, choiceContext);

        if (!wasEnlightened && DivinerCombatRuntime.HasEnlightenmentEffect(player))
        {
            await PlayerCmd.GainEnergy(2, player);
        }
    }
}

public class BloodOfTheMartyr : DivinerPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.AnyTime;

    public override bool CanBeGeneratedInCombat => false;

    public override List<(string, string)>? Localization => DivinerLoc.Potion(
        "Blood of the Martyr",
        "Heal to full HP. Lose 5 Destiny.",
        "殉道者之血",
        "回复至满生命。失去 5 点命运。"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null)
        {
            return;
        }

        decimal missingHp = Math.Max(0, player.Creature.MaxHp - player.Creature.CurrentHp);
        if (missingHp > 0)
        {
            await CreatureCmd.Heal(player.Creature, missingHp, true);
        }

        DestinyService.AddDestiny(-5);
        DestinyService.PersistCurrentState(player.RunState);
        await DivinerStatusPowerSync.Sync(player, choiceContext);
    }
}
