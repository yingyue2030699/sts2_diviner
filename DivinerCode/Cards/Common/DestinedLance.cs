using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class DestinedLance : DivinerCard
{
    public DestinedLance()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(16, 5);
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Destined Lance",
        "#Deal !Damage! damage. Good Omen: hit all enemies.",
        "命定长枪",
        "#造成 !Damage! 点伤害。吉兆：改为命中所有敌人。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!DestinyService.IsGoodOmen())
        {
            await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
            return;
        }

        var enemies = CombatState?.HittableEnemies.Where(creature => creature.Side != Owner.Creature.Side).ToList() ?? [];
        foreach (var enemy in enemies)
        {
            await CommonActions.CardAttack(this, enemy).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
