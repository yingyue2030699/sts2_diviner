using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Diviner.DivinerCode.Cards.Common;

public class ThreadCut : DivinerCard
{
    public ThreadCut()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Thread Cut",
        "#Deal !Damage! damage. If the target has Weak, deal 10 more.",
        "断线",
        "#造成 !Damage! 点伤害。如果目标有虚弱，额外造成 10 点伤害。",
        ("upgradedDesc", "#Deal !Damage! damage. If the target has Weak, deal 14 more.", "#造成 !Damage! 点伤害。如果目标有虚弱，额外造成 14 点伤害。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (cardPlay.Target != null && HasWeak(cardPlay.Target))
        {
            await CommonActions.CardAttack(this, cardPlay.Target, IsUpgraded ? 14m : 10m).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
    }

    private static bool HasWeak(Creature target)
    {
        return target.Powers.Any(power => power is WeakPower);
    }
}
