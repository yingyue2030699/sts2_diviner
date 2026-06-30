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
        WithDamage(4, 0);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Crossed Lines",
        "#Deal !Damage! damage. If you have Divinated this run, apply 1 Weak.",
        "交错命线",
        "#造成 !Damage! 点伤害。如果你在本局中占卜过，给予 1 层虚弱。",
        ("upgradedDesc", "#Deal !Damage! damage. If you have Divinated this run, apply 2 Weak.", "#造成 !Damage! 点伤害。如果你在本局中占卜过，给予 2 层虚弱。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (DivinationService.CurrentRecords.Count > 0 && cardPlay.Target != null)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, IsUpgraded ? 2 : 1, Owner.Creature, this, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
