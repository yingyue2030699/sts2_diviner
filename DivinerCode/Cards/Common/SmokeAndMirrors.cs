using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class SmokeAndMirrors : DivinerCard
{
    public SmokeAndMirrors()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithBlock(9, 2);
        WithDivinerKeywordTips(DivinerKeywords.Foretell);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Smoke and Mirrors",
        "Gain !Block! Block. The next Foretell effect you play this combat resolves with 7 more damage or Block.",
        "烟幕幻镜",
        "获得 !Block! 点格挡。你本场战斗中打出的下一个预言效果结算时，伤害或格挡增加 7 点。",
        ("upgradedDesc", "Gain !Block! Block. The next Foretell effect you play this combat resolves with 9 more damage or Block.", "获得 !Block! 点格挡。你本场战斗中打出的下一个预言效果结算时，伤害或格挡增加 9 点。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        DivinerCombatRuntime.SetNextForetellDamageOrBlockBonus(IsUpgraded ? 9 : 7);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
    }
}
