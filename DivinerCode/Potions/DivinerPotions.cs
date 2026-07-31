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
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override List<(string, string)>? Localization => DivinerLoc.Potion(
        "Tar of Dread",
        "Fill your hand with Misfortune.",
        "恐惧焦油",
        "用噩运填满你的手牌。"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null)
        {
            return;
        }

        var hand = PileType.Hand.GetPile(player);
        int cardsToAdd = Math.Max(0, CardPile.MaxCardsInHand - hand.Cards.Count);
        if (cardsToAdd > 0)
        {
            await DivinerCardActions.AddGeneratedToCombat<Misfortune>(player, cardsToAdd, PileType.Hand, CardPilePosition.Bottom);
        }
    }
}

public class BrewOfBrew : DivinerPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.AnyTime;

    public override bool CanBeGeneratedInCombat => false;

    public override List<(string, string)>? Localization => DivinerLoc.Potion(
        "Brew of Brew",
        "Heal 5 HP and discard all other potions. For each potion discarded, heal 5 extra HP and gain 1 Destiny.",
        "酿中酿",
        "回复 5 点生命并丢弃所有其他药水。每因此丢弃 1 瓶药水，额外回复 5 点生命并获得 1 点命运。"
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
        if (otherPotions.Count > 0)
        {
            DestinyService.AddDestiny(player, otherPotions.Count);
            DestinyService.PersistCurrentState(player);
            await DivinerStatusPowerSync.Sync(player, choiceContext);
        }
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

        player.RunState.Rng.CombatCardSelection.Shuffle(drawCards);
        var candidates = drawCards
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
        "Add a Misfortune+ to your hand.",
        "凝缩噩运",
        "将 1 张噩运+加入你的手牌。"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null)
        {
            return;
        }

        await DivinerCardActions.AddGeneratedToCombat<Misfortune>(
            player,
            PileType.Hand,
            CardPilePosition.Bottom,
            upgraded: true);
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
        DestinyService.AddDestiny(player, 2);
        DestinyService.PersistCurrentState(player);
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
        "Heal 60% of Max HP. Set Destiny to 0.",
        "殉道者之血",
        "回复最大生命的 60%。将命运设为 0。"
    );

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var player = ResolvePlayer(target);
        if (player == null)
        {
            return;
        }

        decimal healing = Math.Ceiling(player.Creature.MaxHp * 0.6m);
        if (healing > 0)
        {
            await CreatureCmd.Heal(player.Creature, healing, true);
        }

        DestinyService.SetDestiny(player, 0);
        DestinyService.PersistCurrentState(player);
        await DivinerStatusPowerSync.Sync(player, choiceContext);
    }
}
