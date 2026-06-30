using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Diviner.DivinerCode.Cards.Common;

public class ForewarnedBlow : DivinerCard
{
    public ForewarnedBlow()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(14, 4);
        WithCards(1, 1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Forewarned Blow",
        "Deal !Damage! damage. If the target has Weak, draw !Cards! card.",
        "先兆打击",
        "造成 !Damage! 点伤害。如果目标有虚弱，抽 !Cards! 张牌。",
        ("upgradedDesc", "Deal !Damage! damage. If the target has Weak, draw !Cards! cards.", "造成 !Damage! 点伤害。如果目标有虚弱，抽 !Cards! 张牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (cardPlay.Target != null && HasWeak(cardPlay.Target))
        {
            await CardPileCmd.Draw(choiceContext, IsUpgraded ? 2 : 1, Owner, false);
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
