using Diviner.DivinerCode.Cards;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Relics;

public static class DivinerRelicHooks
{
    private static readonly Dictionary<Player, int> LastObservedDestinyByPlayer = [];

    static DivinerRelicHooks()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += LastObservedDestinyByPlayer.Clear;
    }

    public static bool HasRelic<TRelic>(Player? player) where TRelic : RelicModel
    {
        return player?.Relics.Any(relic => relic is TRelic) == true;
    }

    public static async Task AfterDivination(PlayerChoiceContext? choiceContext, Player player, int combatDivinationCount)
    {
        if (choiceContext != null && combatDivinationCount == 1)
        {
            if (HasRelic<CloudedLens>(player))
            {
                await CardPileCmd.Draw(choiceContext, 2, player, false);
            }

            if (HasRelic<ProphetsQuill>(player))
            {
                await PowerCmd.Apply<StrengthPower>(choiceContext, player.Creature, 1, player.Creature, null!, false);
                await PowerCmd.Apply<DexterityPower>(choiceContext, player.Creature, 1, player.Creature, null!, false);
            }
        }

        int recordCount = DivinationService.GetRecords(player).Count;
        if (HasRelic<BloodTablet>(player) &&
            recordCount > 0 &&
            recordCount % 5 == 0)
        {
            await CreatureCmd.Heal(player.Creature, 5, true);
        }
    }

    public static async Task OnStatusSync(Player player, PlayerChoiceContext? choiceContext)
    {
        int destiny = DestinyService.GetDestiny(player);
        if (!LastObservedDestinyByPlayer.TryGetValue(player, out var previous))
        {
            LastObservedDestinyByPlayer[player] = destiny;
            return;
        }

        if (previous == destiny)
        {
            return;
        }

        LastObservedDestinyByPlayer[player] = destiny;
        if (choiceContext != null && HasRelic<BrassDowsingRod>(player))
        {
            await CreatureCmd.GainBlock(player.Creature, 7, BlockProps.cardUnpowered, null, false);
        }
    }

    public static int ForetellDamageOrBlockBonus(Player? player)
    {
        return HasRelic<PiedraDelSol>(player) ? 2 : 0;
    }

    public static int DredgeStartingCountdown(Player? player)
    {
        return HasRelic<HourglassOfMercy>(player) ? 5 : DestinyConstants.DredgeStartingCountdown;
    }

    public static bool IsFirstEscapeFree(Player? player)
    {
        return HasRelic<HourglassOfMercy>(player) && DivinerCombatRuntime.EscapeCardsPlayedThisCombatFor(player) == 0;
    }

    public static int EnlightenmentThresholdReduction(Player? player)
    {
        return HasRelic<FixedStarMap>(player) ? 1 : 0;
    }

    public static bool SuppressesPositiveRewardRarity(Player? player)
    {
        return HasRelic<FatedContract>(player) && DestinyConstants.Clamp(DestinyService.GetDestiny(player)) >= 4;
    }

    public static float PotionDropBonus(Player? player)
    {
        if (!HasRelic<VelvetPouch>(player))
        {
            return 0f;
        }

        return DestinyService.IsGoodOmen(player) ? 0.8f : 0.4f;
    }
}

public class CloudedLens : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Clouded Lens",
        "The first time you Divinate each combat, draw 2 cards.",
        "The fog clears just enough to see the next mistake.",
        "雾蚀透镜",
        "每场战斗中你第一次占卜时，抽 2 张牌。",
        "雾气稍稍散开，刚好看清下一次错误。"
    );
}

public class BrassDowsingRod : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Brass Dowsing Rod",
        "Whenever your Destiny changes, gain 7 Block.",
        "It trembles at the exact frequency of bad decisions.",
        "黄铜探命杆",
        "每当你的命运变化时，获得 7 点格挡。",
        "它以糟糕决定的频率微微颤动。"
    );
}

public class BloodTablet : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Blood Tablet",
        "Every 5th recorded Divination heals 5 HP.",
        "The oldest prophecies are written in the body.",
        "血字泥板",
        "每记录第 5 次占卜时，回复 5 点生命。",
        "最古老的预言写在血肉之中。"
    );
}

public class PiedraDelSol : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Piedra del Sol",
        "Foretell effects that deal damage or gain Block do 2 more.",
        "A small sun caught in patient stone.",
        "太阳石",
        "造成伤害或获得格挡的预言效果数值提高 2。",
        "一轮小太阳被耐心地困在石中。"
    );
}

public class KnockedCompass : DivinerRelic
{
    private ICombatState? _lastCombatState;
    private bool _applied;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Knocked Compass",
        "At the start of combat, if Bad Omen, apply 2 Weak to all enemies.",
        "It points south. Then south again.",
        "敲歪的罗盘",
        "战斗开始时，若为凶兆，给予所有敌人 2 层虚弱。",
        "它指向南方。然后还是南方。"
    );

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!ReferenceEquals(_lastCombatState, combatState))
        {
            _lastCombatState = combatState;
            _applied = false;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (_applied || !ReferenceEquals(player, Owner))
        {
            return;
        }

        _applied = true;
        DestinyService.EnsureLoadedForPlayer(player);
        if (!DestinyService.IsBadOmen(player))
        {
            return;
        }

        Flash();
        foreach (var enemy in DivinerCombatRuntime.HittableEnemiesFor(player))
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, 2, player.Creature, null!, false);
        }
    }
}

public class MarkedDeck : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Marked Deck",
        "Card rewards have 1 additional option while Good Omen.",
        "Every back has a little tell.",
        "记号牌组",
        "处于吉兆时，卡牌奖励多 1 个选项。",
        "每张牌背都有一点破绽。"
    );

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        // The late pass runs for every reward list and avoids modifying the same
        // mutable list in both hook phases.
        return false;
    }

    public override bool TryModifyCardRewardOptionsLate(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        return TryAddGoodOmenCardRewardOption(player, cardRewardOptions, creationOptions);
    }

    private bool TryAddGoodOmenCardRewardOption(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        try
        {
            if (!ReferenceEquals(Owner, player))
            {
                return false;
            }

            DestinyService.EnsureLoadedForPlayer(player);
            if (!DestinyService.IsGoodOmen(player))
            {
                return false;
            }

            int originalCount = cardRewardOptions.Count;
            var extraOptions = CardRewardOptionHelper.CreateExtraOptionsFromCurrentReward(
                player,
                cardRewardOptions,
                creationOptions,
                1);
            if (extraOptions.Count == 0)
            {
                return false;
            }

            cardRewardOptions.AddRange(extraOptions);
            MainFile.Logger.Info(
                $"Diviner Marked Deck modified reward: before={originalCount}, added={extraOptions.Count}, after={cardRewardOptions.Count}, player={player.NetId}.");
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner Marked Deck failed to add a reward option: {ex}");
            return false;
        }
    }
}

public class ProphetsQuill : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Prophet's Quill",
        "The first time you Divinate each combat, gain 1 Strength and 1 Dexterity.",
        "It writes before you think.",
        "先知羽笔",
        "每场战斗中你第一次占卜时，获得 1 点力量和 1 点敏捷。",
        "它在你思考之前就写下答案。"
    );
}

public class SealedEnvelope : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool HasUponPickupEffect => true;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Sealed Envelope",
        "On pickup, Divinate 3 times and gain 1 Destiny.",
        "Only open after it already mattered.",
        "封蜡信封",
        "拾取时，占卜 3 次并获得 1 点命运。",
        "等一切已经重要之后再打开。"
    );

    public override async Task AfterObtained()
    {
        for (int i = 0; i < 3; i++)
        {
            await DivinationService.RecordPlaceholder(Owner, "Sealed Envelope");
        }

        DestinyService.AddDestiny(Owner, 1);
        DestinyService.PersistCurrentState(Owner);
    }
}

public class HourglassOfMercy : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Hourglass of Mercy",
        "Doomed starts with 5 Countdown of Destiny instead of 3. The first Escape from Destiny each combat costs 0.",
        "A few grains have been taught pity.",
        "慈悲沙漏",
        "劫兆以 5 层命运倒计时开始，而非 3 层。每场战斗第一张逃离命运耗能为 0。",
        "有几粒沙学会了怜悯。"
    );
}

public class OracleBone : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool HasUponPickupEffect => true;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Oracle Bone",
        "On pickup, gain 5 Destiny.",
        "Cracked once. Read forever.",
        "甲骨",
        "拾取时，获得 5 点命运。",
        "裂过一次，便可永读。"
    );

    public override Task AfterObtained()
    {
        DestinyService.AddDestiny(Owner, 5);
        DestinyService.PersistCurrentState(Owner);
        return Task.CompletedTask;
    }
}

public class FixedStarMap : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Fixed Star Map",
        "Cards with Revelation effects can trigger them with 1 less Destiny.",
        "A chart of stars that refuse to drift.",
        "恒星图",
        "卡牌的启示效果可以用低 1 点的命运触发。",
        "一张绘着拒绝漂移之星的图。"
    );
}

public class LastProphecy : DivinerRelic
{
    private static readonly HashSet<Player> TriggeredThisCombat = [];

    static LastProphecy()
    {
        DivinerCombatRuntime.CombatOnlyStateReset += TriggeredThisCombat.Clear;
    }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Last Prophecy",
        "Once per combat, if you would die with at least 8 recorded Divinations, delete up to 9 records and heal to 1 HP.",
        "The final sentence is always conditional.",
        "最后预言",
        "每场战斗一次，若你将要死亡且至少记录了 8 次占卜，删除至多 9 条记录并回复至 1 点生命。",
        "最后一句总是带有条件。"
    );

    public override decimal ModifyHpLostAfterOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner.Creature ||
            TriggeredThisCombat.Contains(Owner) ||
            amount < target.CurrentHp ||
            DivinationService.GetRecords(Owner).Count < 8)
        {
            return amount;
        }

        int recordsToDelete = Math.Min(9, DivinationService.GetRecords(Owner).Count);
        if (!DivinationService.TryConsumeRecords(Owner, recordsToDelete, Owner.RunState))
        {
            return amount;
        }

        TriggeredThisCombat.Add(Owner);
        Flash();
        return Math.Max(0, target.CurrentHp - 1);
    }
}

public class VelvetPouch : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool HasUponPickupEffect => true;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Velvet Pouch",
        "Gain 1 Potion slot. Potions are 40% cheaper and potion drop rate is increased by 40%; these effects are doubled in Good Omen.",
        "Soft enough to keep the future from rattling.",
        "天鹅绒小袋",
        "获得 1 个药水栏位。药水便宜 40%，药水掉落率提高 40%；处于吉兆时这些效果翻倍。",
        "柔软到足以让未来不再作响。"
    );

    public override async Task AfterObtained()
    {
        await PlayerCmd.GainMaxPotionCount(1, Owner);
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
    {
        if (!ReferenceEquals(player, Owner) || entry is not MerchantPotionEntry)
        {
            return cost;
        }

        decimal multiplier = DestinyService.IsGoodOmen(player) ? 0.2m : 0.6m;
        return Math.Max(0, Math.Ceiling(cost * multiplier));
    }
}

public class FatedContract : DivinerRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool HasUponPickupEffect => true;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Fated Contract",
        "On pickup and at the start of each act, set Destiny to 5. Destiny of 4 or more no longer improves reward rarity.",
        "The future signs first, then asks.",
        "命定契约",
        "拾取时以及每幕开始时，将命运设为 5。命运为 4 或更高时不再提高奖励稀有度。",
        "未来先签名，再开口询问。"
    );

    public override Task AfterObtained()
    {
        SetDestinyToFive();
        return Task.CompletedTask;
    }

    public override Task AfterActEntered()
    {
        SetDestinyToFive();
        return Task.CompletedTask;
    }

    private void SetDestinyToFive()
    {
        DestinyService.SetDestiny(Owner, DestinyConstants.EnlightenmentDestiny);
        DestinyService.PersistCurrentState(Owner);
    }
}
