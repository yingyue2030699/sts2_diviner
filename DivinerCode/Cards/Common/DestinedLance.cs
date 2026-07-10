using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards.Common;

public class DestinedLance : DivinerCard
{
    public DestinedLance()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(16, 4);
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen, DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Destined Lance",
        "Deal !Damage! damage. Good Omen: hit all enemies. Bad Omen: damage increased by 10.",
        "命定长枪",
        "造成 !Damage! 点伤害。吉兆：改为命中所有敌人。凶兆：伤害提高 10 点。",
        ("upgradedDesc", "Deal !Damage! damage. Good Omen: hit all enemies. Bad Omen: damage increased by 12.", "造成 !Damage! 点伤害。吉兆：改为命中所有敌人。凶兆：伤害提高 12 点。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int damage = (IsUpgraded ? 20 : 16) + (DestinyService.IsBadOmen() ? (IsUpgraded ? 12 : 10) : 0);
        if (DestinyService.IsGoodOmen())
        {
            var enemies = CombatState?.HittableEnemies.Where(creature => creature.Side != Owner.Creature.Side).ToList() ?? [];
            foreach (var enemy in enemies)
            {
                await CreatureCmd.Damage(choiceContext, enemy, damage, DamageProps.cardUnpowered, Owner.Creature, this);
            }

            return;
        }

        if (cardPlay.Target != null)
        {
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, damage, DamageProps.cardUnpowered, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
