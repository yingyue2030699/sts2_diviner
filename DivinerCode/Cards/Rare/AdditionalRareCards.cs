using BaseLib.Abstracts;
using Diviner.DivinerCode.Cards;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Rare;

public class MomentOfReckoning : DivinerCard
{
    public MomentOfReckoning()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithDamage(10, 3);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Moment of Reckoning",
        "Trigger all Foretell effects immediately. For each Foretell effect triggered, deal !Damage! damage to all enemies.",
        "清算时刻",
        "立即触发所有预言效果。每触发一个预言效果，对所有敌人造成 !Damage! 点伤害。",
        ("upgradedDesc", "Trigger all Foretell effects immediately. For each Foretell effect triggered, deal !Damage! damage to all enemies.", "立即触发所有预言效果。每触发一个预言效果，对所有敌人造成 !Damage! 点伤害。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int triggerCount = await DivinerCombatRuntime.TriggerAllForetellNow(choiceContext, Owner);
        if (triggerCount <= 0 || CombatState == null)
        {
            return;
        }

        await using var attack = await AttackCommand.CreateContextAsync(CombatState, choiceContext, this);
        for (int i = 0; i < triggerCount; i++)
        {
            var enemies = DivinerCardActions.HittableEnemies(this);
            if (enemies.Count == 0)
            {
                return;
            }

            var results = await CreatureCmd.Damage(
                choiceContext,
                enemies,
                IsUpgraded ? 13 : 10,
                DamageProps.card,
                Owner.Creature,
                this);
            attack.AddHit(results);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

public class RelicBanishing : DivinerCard
{
    public RelicBanishing()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Relic Banishing",
        "Divinate twice for relic divinations only. Add a Bend Future to your hand.",
        "遗物驱离",
        "仅进行遗物占卜两次。将一张扭转未来加入你的手牌。",
        ("upgradedDesc", "Divinate twice for relic divinations only. Add a Bend Future+ to your hand.", "仅进行遗物占卜两次。将一张扭转未来+加入你的手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinationService.RecordRelicDivination(choiceContext, Owner, "Relic Banishing");
        await DivinationService.RecordRelicDivination(choiceContext, Owner, "Relic Banishing");
        await DivinerCardActions.AddGeneratedToCombat<BendFuture>(this, PileType.Hand, CardPilePosition.Bottom, IsUpgraded);
    }

    protected override void OnUpgrade()
    {
    }
}

public class TheFinalStrand : DivinerCard
{
    public TheFinalStrand()
        : base(0, CardType.Power, CardRarity.Rare, TargetType.TargetedNoCreature)
    {
        WithKeyword(CardKeyword.Innate, UpgradeType.Add);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "The Final Strand",
        "Lose 5 Destiny. Revelation effects always trigger regardless of Destiny this combat.",
        "终末命缕",
        "失去 5 点命运。本场战斗中，启示效果总是触发，无视命运。",
        ("upgradedDesc", "Innate. Lose 5 Destiny. Revelation effects always trigger regardless of Destiny this combat.", "固有。失去 5 点命运。本场战斗中，启示效果总是触发，无视命运。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DestinyService.AddDestiny(Owner, -5);
        DestinyService.PersistCurrentState(Owner);
        DivinerCombatRuntime.ForceRevelationEffectsThisCombat(Owner);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}
