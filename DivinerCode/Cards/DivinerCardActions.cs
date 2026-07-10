using BaseLib.Extensions;
using BaseLib.Utils;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Diviner.DivinerCode.Cards;

public static class DivinerCardActions
{
    public static async Task Scry(DivinerCard source, PlayerChoiceContext choiceContext, int count)
    {
        count += ManyFuturesPower.ExtraScryCards(source.Owner);
        var topCards = PileType.Draw.GetPile(source.Owner).Cards.Take(count).ToList();
        if (topCards.Count == 0)
        {
            return;
        }

        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            topCards,
            source.Owner,
            new CardSelectorPrefs(new LocString("cards", $"{source.Id.Entry}.selectPrompt"), 0, topCards.Count)
        );
        var selectedCards = selected.ToList();
        if (selectedCards.Count == 0)
        {
            return;
        }

        await CardPileCmd.Add(selectedCards, PileType.Discard, CardPilePosition.Top, source, false);
    }

    public static IReadOnlyList<CardModel> DrawPileCards(Player player)
    {
        return PileType.Draw.GetPile(player).Cards.ToList();
    }

    public static IReadOnlyList<CardModel> HandCards(Player player)
    {
        return PileType.Hand.GetPile(player).Cards.ToList();
    }

    public static IReadOnlyList<Creature> HittableEnemies(DivinerCard source)
    {
        return source.CombatState?.HittableEnemies
            .Where(creature => creature.Side != source.Owner.Creature.Side)
            .ToList() ?? [];
    }

    public static bool HasWeakOrVulnerable(Creature creature)
    {
        return creature.Powers.Any(power => power is WeakPower or VulnerablePower);
    }

    public static async Task ApplyWeakAndVulnerable(
        DivinerCard source,
        PlayerChoiceContext choiceContext,
        Creature target,
        int amount)
    {
        await PowerCmd.Apply<WeakPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, target, amount, source.Owner.Creature, source, false);
    }

    public static async Task ApplyWeakAndVulnerableToAll(
        DivinerCard source,
        PlayerChoiceContext choiceContext,
        int amount)
    {
        foreach (var enemy in HittableEnemies(source))
        {
            await ApplyWeakAndVulnerable(source, choiceContext, enemy, amount);
        }
    }

    public static async Task<IReadOnlyList<CardModel>> SelectFromDrawPile(
        DivinerCard source,
        PlayerChoiceContext choiceContext,
        string promptKey,
        int min,
        int max,
        Func<CardModel, bool>? filter = null)
    {
        var selectableCards = DrawPileCards(source.Owner)
            .Where(card => filter?.Invoke(card) ?? true)
            .ToList();
        if (selectableCards.Count == 0 || max <= 0)
        {
            return [];
        }

        int adjustedMax = Math.Min(max, selectableCards.Count);
        int adjustedMin = Math.Min(min, adjustedMax);
        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            selectableCards,
            source.Owner,
            new CardSelectorPrefs(new LocString("cards", $"{source.Id.Entry}.{promptKey}"), adjustedMin, adjustedMax)
        );

        return selected.Take(adjustedMax).ToList();
    }

    public static async Task<IReadOnlyList<CardModel>> SelectFromHand(
        DivinerCard source,
        PlayerChoiceContext choiceContext,
        string promptKey,
        int min,
        int max,
        Func<CardModel, bool>? filter = null)
    {
        var selectableCards = HandCards(source.Owner)
            .Where(card => !ReferenceEquals(card, source) && (filter?.Invoke(card) ?? true))
            .ToList();
        if (selectableCards.Count == 0 || max <= 0)
        {
            return [];
        }

        int adjustedMax = Math.Min(max, selectableCards.Count);
        int adjustedMin = Math.Min(min, adjustedMax);
        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            selectableCards,
            source.Owner,
            new CardSelectorPrefs(new LocString("cards", $"{source.Id.Entry}.{promptKey}"), adjustedMin, adjustedMax)
        );

        return selected.Take(adjustedMax).ToList();
    }

    public static void MakeFated(CardModel card)
    {
        card.EnergyCost.SetThisTurnOrUntilPlayed(0, false);
        DivinerCombatRuntime.MarkCardFreeThisTurn(card);
    }

    public static async Task MoveToHandFated(DivinerCard source, IEnumerable<CardModel> cards)
    {
        var selectedCards = cards.ToList();
        foreach (var card in selectedCards)
        {
            MakeFated(card);
        }

        if (selectedCards.Count > 0)
        {
            await CardPileCmd.Add(selectedCards, PileType.Hand, CardPilePosition.Bottom, source, false);
        }
    }

    public static async Task AddGeneratedToCombat<TCard>(
        DivinerCard source,
        PileType pileType,
        CardPilePosition position)
        where TCard : CardModel
    {
        if (DivinerCombatRuntime.CombatState == null)
        {
            MainFile.Logger.Error($"Diviner could not create generated {typeof(TCard).Name}: no combat state tracked.");
            return;
        }

        var createdCard = DivinerCombatRuntime.CombatState.CreateCard(ModelDb.Card<TCard>(), source.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(createdCard, pileType, source.Owner, position);
    }

    public static async Task AddGeneratedToCombat<TCard>(
        DivinerCard source,
        PileType pileType,
        CardPilePosition position,
        bool upgraded)
        where TCard : CardModel
    {
        if (DivinerCombatRuntime.CombatState == null)
        {
            MainFile.Logger.Error($"Diviner could not create generated {typeof(TCard).Name}: no combat state tracked.");
            return;
        }

        var createdCard = DivinerCombatRuntime.CombatState.CreateCard(ModelDb.Card<TCard>(), source.Owner);
        if (upgraded)
        {
            CardCmd.Upgrade(createdCard);
        }

        await CardPileCmd.AddGeneratedCardToCombat(createdCard, pileType, source.Owner, position);
    }

    public static async Task AddGeneratedToCombat<TCard>(
        Player player,
        PileType pileType,
        CardPilePosition position)
        where TCard : CardModel
    {
        if (DivinerCombatRuntime.CombatState == null)
        {
            MainFile.Logger.Error($"Diviner could not create generated {typeof(TCard).Name}: no combat state tracked.");
            return;
        }

        var createdCard = DivinerCombatRuntime.CombatState.CreateCard(ModelDb.Card<TCard>(), player);
        await CardPileCmd.AddGeneratedCardToCombat(createdCard, pileType, player, position);
    }

    public static async Task AddGeneratedToCombat<TCard>(
        Player player,
        PileType pileType,
        CardPilePosition position,
        bool upgraded)
        where TCard : CardModel
    {
        if (DivinerCombatRuntime.CombatState == null)
        {
            MainFile.Logger.Error($"Diviner could not create generated {typeof(TCard).Name}: no combat state tracked.");
            return;
        }

        var createdCard = DivinerCombatRuntime.CombatState.CreateCard(ModelDb.Card<TCard>(), player);
        if (upgraded)
        {
            CardCmd.Upgrade(createdCard);
        }

        await CardPileCmd.AddGeneratedCardToCombat(createdCard, pileType, player, position);
    }

    public static async Task AddGeneratedToCombat<TCard>(
        Player player,
        int count,
        PileType pileType,
        CardPilePosition position)
        where TCard : CardModel
    {
        for (int i = 0; i < count; i++)
        {
            await AddGeneratedToCombat<TCard>(player, pileType, position);
        }
    }

    public static async Task AddGeneratedToCombat<TCard>(
        DivinerCard source,
        int count,
        PileType pileType,
        CardPilePosition position)
        where TCard : CardModel
    {
        for (int i = 0; i < count; i++)
        {
            await AddGeneratedToCombat<TCard>(source, pileType, position);
        }
    }

    public static async Task DrawUntilHandSize(
        DivinerCard source,
        PlayerChoiceContext choiceContext,
        int handSize)
    {
        int cardsToDraw = Math.Max(0, handSize - HandCards(source.Owner).Count);
        if (cardsToDraw > 0)
        {
            await CardPileCmd.Draw(choiceContext, cardsToDraw, source.Owner, false);
        }
    }

    public static bool IsDivinerGeneratedCard(CardModel card)
    {
        return card is Fortune or Misfortune or EscapeFromDestiny;
    }
}
