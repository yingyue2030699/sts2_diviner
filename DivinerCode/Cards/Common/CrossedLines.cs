using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Diviner.DivinerCode.Cards.Common;

public class CrossedLines : DivinerCard
{
    public CrossedLines()
        : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(3, 1);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Crossed Lines",
        "Deal !Damage! damage. If you have Divinated this combat, apply 3 Weak.",
        "交错命线",
        "造成 !Damage! 点伤害。如果你在本场战斗中占卜过，给予 3 层虚弱。",
        ("upgradedDesc", "Deal !Damage! damage. If you have Divinated this combat, apply 4 Weak.", "造成 !Damage! 点伤害。如果你在本场战斗中占卜过，给予 4 层虚弱。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (DivinerCombatRuntime.HasDivinatedThisCombat && cardPlay.Target != null)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, IsUpgraded ? 4 : 3, Owner.Creature, this, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
