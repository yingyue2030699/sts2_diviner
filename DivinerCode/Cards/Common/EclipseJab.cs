using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class EclipseJab : DivinerCard
{
    public EclipseJab()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(10, 3);
        WithDivinerKeywordTips(DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Eclipse Jab",
        "Deal !Damage! damage. Bad Omen: then deal 8 damage to all enemies.",
        "蚀月刺击",
        "造成 !Damage! 点伤害。凶兆：然后对所有敌人造成 8 点伤害。",
        ("upgradedDesc", "Deal !Damage! damage. Bad Omen: then deal 11 damage to all enemies.", "造成 !Damage! 点伤害。凶兆：然后对所有敌人造成 11 点伤害。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        if (!DestinyService.IsBadOmen())
        {
            return;
        }

        var enemies = DivinerCardActions.HittableEnemies(this);
        foreach (var enemy in enemies)
        {
            await CommonActions.CardAttack(this, enemy, IsUpgraded ? 11m : 8m).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
