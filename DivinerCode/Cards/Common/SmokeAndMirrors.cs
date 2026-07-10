using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class SmokeAndMirrors : DivinerCard
{
    public SmokeAndMirrors()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithBlock(8, 2);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Smoke and Mirrors",
        "Gain !Block! Block. The next Foretell effect you play this combat resolves with +8 damage or Block.",
        "烟幕幻镜",
        "获得 !Block! 点格挡。你本场战斗中打出的下一个预言效果结算时，伤害或格挡 +8。",
        ("upgradedDesc", "Gain !Block! Block. The next Foretell effect you play this combat resolves with +10 damage or Block.", "获得 !Block! 点格挡。你本场战斗中打出的下一个预言效果结算时，伤害或格挡 +10。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        DivinerCombatRuntime.SetNextForetellDamageOrBlockBonus(IsUpgraded ? 10 : 8);
    }

    protected override void OnUpgrade()
    {
    }
}
