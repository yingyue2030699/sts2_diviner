using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class LineOfFate : DivinerCard
{
    public LineOfFate()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(6, 0);
        WithKeywords([CardKeyword.Exhaust]);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Line of Fate",
        "#Deal !Damage! damage. If Fatal, Divinate.",
        "命运之线",
        "#造成 !Damage! 点伤害。如果致命，占卜。",
        ("upgradedDesc", "#Deal !Damage! damage. If Fatal, Divinate.", "#造成 !Damage! 点伤害。如果致命，占卜。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        if (target != null && !target.IsAlive)
        {
            await DivinationService.RecordPlaceholder(choiceContext, Owner, "Line of Fate");
        }
    }

    protected override void OnUpgrade()
    {
    }
}
