using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Diviner.DivinerCode.Mechanics;

public static class CardRewardOptionHelper
{
    public static List<CardCreationResult> CreateExtraOptionsFromCurrentReward(
        Player player,
        IReadOnlyCollection<CardCreationResult> currentOptions,
        CardCreationOptions creationOptions,
        int count)
    {
        if (count <= 0)
        {
            return [];
        }

        var currentIds = currentOptions
            .Select(option => option.Card.Id)
            .ToHashSet();
        var currentPoolIds = currentOptions
            .Select(option => option.Card.Pool?.Id)
            .Where(id => id != null)
            .ToHashSet();

        var possibleCards = creationOptions
            .GetPossibleCards(player)
            .Where(card => !currentIds.Contains(card.Id));
        if (currentPoolIds.Count > 0)
        {
            possibleCards = possibleCards.Where(card => currentPoolIds.Contains(card.Pool.Id));
        }

        var scopedPool = possibleCards.ToList();
        if (scopedPool.Count == 0)
        {
            return [];
        }

        var scopedOptions = creationOptions
            .WithCustomPool(scopedPool, creationOptions.RarityOdds)
            .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications);

        var extraOptions = CardFactory
            .CreateForReward(player, count, scopedOptions)
            .Where(option => !currentIds.Contains(option.Card.Id))
            .Take(count)
            .ToList();

        return extraOptions;
    }
}
