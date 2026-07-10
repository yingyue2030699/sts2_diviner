namespace Diviner.DivinerCode.Mechanics;

using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

public static class DestinyRewardTuning
{
    public static decimal LuckMultiplier(int destiny)
    {
        return 1m;
    }

    public static decimal UpgradeOddsMultiplier(int destiny)
    {
        return 1m;
    }

    public static float AdjustmentChance(int destiny)
    {
        return HasRewardRarityShift(destiny) ? 0.5f : 0f;
    }

    public static bool IsPositiveLuck(int destiny)
    {
        return DestinyConstants.Clamp(destiny) >= 4;
    }

    public static CardRarity AdjustCardRarity(CardRarity rarity, int destiny, Func<float, bool> roll, bool suppressPositiveShift = false)
    {
        var clamped = DestinyConstants.Clamp(destiny);
        if (!suppressPositiveShift && clamped >= 4 && rarity == CardRarity.Common && roll(0.5f))
        {
            return PromoteCardRarity(rarity, roll);
        }

        if (clamped <= 1 && rarity is CardRarity.Uncommon or CardRarity.Rare && roll(0.5f))
        {
            return CardRarity.Common;
        }

        return rarity;
    }

    public static RelicRarity AdjustRelicRarity(RelicRarity rarity, int destiny, Func<float, bool> roll, bool suppressPositiveShift = false)
    {
        var clamped = DestinyConstants.Clamp(destiny);
        if (!suppressPositiveShift && clamped >= 4 && rarity == RelicRarity.Common && roll(0.5f))
        {
            return PromoteRelicRarity(rarity, roll);
        }

        if (clamped <= 1 && rarity is RelicRarity.Uncommon or RelicRarity.Rare && roll(0.5f))
        {
            return RelicRarity.Common;
        }

        return rarity;
    }

    public static bool AdjustPotionRoll(bool original, int destiny, Func<float, bool> roll, Player? player = null)
    {
        if (original)
        {
            return true;
        }

        var pouchBonus = DivinerRelicHooks.PotionDropBonus(player);
        return pouchBonus > 0f && roll(pouchBonus);
    }

    public static RoomType AdjustUnknownRoomType(
        RoomType original,
        int destiny,
        IReadOnlySet<RoomType> blacklist,
        Func<float, bool> roll)
    {
        var clamped = DestinyConstants.Clamp(destiny);
        if (clamped >= 3)
        {
            if (original == RoomType.Monster)
            {
                return FirstAllowedRoomType(blacklist, RoomType.Event, RoomType.Shop, RoomType.Treasure, RoomType.Elite) ??
                       original;
            }

            return original;
        }

        if (clamped <= 2 && !blacklist.Contains(RoomType.Monster))
        {
            return RoomType.Monster;
        }

        return original;
    }

    public static string DescribeLuck(int destiny)
    {
        return DescribeLuckBlock(destiny);
    }

    public static string DescribeLuckBlock(int destiny)
    {
        var clamped = DestinyConstants.Clamp(destiny);
        var lines = new List<string>
        {
            DivinerLoc.Text($"Destiny {clamped}", $"命运 {clamped}")
        };

        if (clamped <= 0)
        {
            lines.Add(DivinerLoc.Text("Doomed: start combat with Countdown of Destiny.", "劫兆：战斗开始时获得命运倒计时。"));
        }

        if (clamped <= 1)
        {
            lines.Add(DivinerLoc.Text("Common card/relic results are 50% more likely.", "普通卡牌/遗物结果提高 50%。"));
        }

        if (clamped <= 2)
        {
            lines.Add(DivinerLoc.Text("Unknown rooms become combat when allowed.", "问号房间在允许时必定变为战斗。"));
        }

        if (clamped >= 3)
        {
            lines.Add(DivinerLoc.Text("Unknown rooms cannot become combat.", "问号房间不会变为战斗。"));
        }

        if (clamped >= 4)
        {
            lines.Add(DivinerLoc.Text("Common card/relic results are cut in half.", "普通卡牌/遗物结果减半。"));
        }

        if (clamped >= 5)
        {
            lines.Add(DivinerLoc.Text("Revelation: search 3 cards at combat start and make them Fated.", "启示：战斗开始时搜寻 3 张牌并使其注定。"));
        }

        return string.Join("\n", lines);
    }

    private static bool HasRewardRarityShift(int destiny)
    {
        var clamped = DestinyConstants.Clamp(destiny);
        return clamped <= 1 || clamped >= 4;
    }

    private static CardRarity PromoteCardRarity(CardRarity rarity, Func<float, bool> roll)
    {
        return rarity switch
        {
            CardRarity.Common => roll(0.25f) ? CardRarity.Rare : CardRarity.Uncommon,
            CardRarity.Uncommon => roll(0.25f) ? CardRarity.Rare : CardRarity.Uncommon,
            _ => rarity
        };
    }

    private static CardRarity DemoteCardRarity(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Rare or CardRarity.Uncommon => CardRarity.Common,
            _ => rarity
        };
    }

    private static RelicRarity PromoteRelicRarity(RelicRarity rarity, Func<float, bool> roll)
    {
        return rarity switch
        {
            RelicRarity.Common => roll(0.25f) ? RelicRarity.Rare : RelicRarity.Uncommon,
            RelicRarity.Uncommon => roll(0.25f) ? RelicRarity.Rare : RelicRarity.Uncommon,
            _ => rarity
        };
    }

    private static RelicRarity DemoteRelicRarity(RelicRarity rarity)
    {
        return rarity switch
        {
            RelicRarity.Rare or RelicRarity.Uncommon => RelicRarity.Common,
            _ => rarity
        };
    }

    private static RoomType? FirstAllowedRoomType(IReadOnlySet<RoomType> blacklist, params RoomType[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!blacklist.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
