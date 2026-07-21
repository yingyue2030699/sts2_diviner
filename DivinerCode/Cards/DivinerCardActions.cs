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
        await AddGeneratedToCombatAtomic<TCard>(source.Owner, pileType, position, false);
    }

    public static async Task AddGeneratedToCombat<TCard>(
        DivinerCard source,
        PileType pileType,
        CardPilePosition position,
        bool upgraded)
        where TCard : CardModel
    {
        await AddGeneratedToCombatAtomic<TCard>(source.Owner, pileType, position, upgraded);
    }

    public static async Task AddGeneratedToCombat<TCard>(
        Player player,
        PileType pileType,
        CardPilePosition position)
        where TCard : CardModel
    {
        await AddGeneratedToCombatAtomic<TCard>(player, pileType, position, false);
    }

    public static async Task AddGeneratedToCombat<TCard>(
        Player player,
        PileType pileType,
        CardPilePosition position,
        bool upgraded)
        where TCard : CardModel
    {
        await AddGeneratedToCombatAtomic<TCard>(player, pileType, position, upgraded);
    }

    private static async Task AddGeneratedToCombatAtomic<TCard>(
        Player player,
        PileType pileType,
        CardPilePosition position,
        bool upgraded)
        where TCard : CardModel
    {
        var combatState = DivinerCombatRuntime.CombatState;
        if (combatState == null)
        {
            MainFile.Logger.Error($"Diviner could not create generated {typeof(TCard).Name}: no combat state tracked.");
            return;
        }

        CardModel? createdCard = null;
        try
        {
            createdCard = combatState.CreateCard(ModelDb.Card<TCard>(), player);
            if (upgraded)
            {
                CardCmd.Upgrade(createdCard);
            }

            var result = await CardPileCmd.AddGeneratedCardToCombat(
                createdCard,
                pileType,
                player,
                position);
            if (!result.success && createdCard.Pile == null && combatState.ContainsCard(createdCard))
            {
                createdCard.RemoveFromState();
                MainFile.Logger.Info(
                    $"Diviner removed unplaced generated card after a rejected add: card={typeof(TCard).Name}, pile={pileType}, player={player.NetId}.");
            }
        }
        catch (Exception ex)
        {
            if (createdCard != null && createdCard.Pile == null && combatState.ContainsCard(createdCard))
            {
                createdCard.RemoveFromState();
            }

            MainFile.Logger.Error(
                $"Diviner generated-card add failed: card={typeof(TCard).Name}, pile={pileType}, player={player.NetId}. {ex}");
            throw;
        }
    }

    public static async Task AddGeneratedToCombat<TCard>(
        Player player,
        int count,
        PileType pileType,
        CardPilePosition position)
        where TCard : CardModel
    {
        int safeCount = Math.Max(0, count);
        if (safeCount == 0)
        {
            return;
        }

        MainFile.Logger.Info(
            $"Diviner generated-card add begin: card={typeof(TCard).Name}, count={safeCount}, pile={pileType}, player={player.NetId}.");
        for (int i = 0; i < safeCount; i++)
        {
            // Create and place each card atomically. Creating the full batch first can
            // leave pileless cards in CombatState if a later pile hook throws.
            await AddGeneratedToCombat<TCard>(player, pileType, position);
        }

        var destinationPile = pileType.GetPile(player);
        destinationPile.InvokeCardAddFinished();
        MainFile.Logger.Info(
            $"Diviner generated-card add complete: card={typeof(TCard).Name}, pile={pileType}, pileCount={destinationPile.Cards.Count}, player={player.NetId}.");
    }

    public static async Task AddGeneratedToCombat<TCard>(
        DivinerCard source,
        int count,
        PileType pileType,
        CardPilePosition position)
        where TCard : CardModel
    {
        int safeCount = Math.Max(0, count);
        if (safeCount == 0)
        {
            return;
        }

        MainFile.Logger.Info(
            $"Diviner generated-card add begin: source={source.Id.Entry}, card={typeof(TCard).Name}, count={safeCount}, pile={pileType}, player={source.Owner.NetId}.");
        for (int i = 0; i < safeCount; i++)
        {
            await AddGeneratedToCombat<TCard>(source, pileType, position);
        }

        var destinationPile = pileType.GetPile(source.Owner);
        destinationPile.InvokeCardAddFinished();
        MainFile.Logger.Info(
            $"Diviner generated-card add complete: source={source.Id.Entry}, card={typeof(TCard).Name}, pile={pileType}, pileCount={destinationPile.Cards.Count}, player={source.Owner.NetId}.");
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
