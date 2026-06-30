using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class PalmReading : DivinerCard
{
    public PalmReading()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithCards(1);
        WithDivinerKeywordTips(DivinerKeywords.Scry, DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Palm Reading",
        "Scry 4. If you divinated this combat, draw !Cards! card.",
        "掌纹解读",
        "预见 4。如果你在本场战斗中占卜过，抽 !Cards! 张牌。",
        ("upgradedDesc", "Scry 6. If you divinated this combat, draw !Cards! card.", "预见 6。如果你在本场战斗中占卜过，抽 !Cards! 张牌。"),
        ("selectPrompt", "Choose cards to discard.", "选择要丢弃的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DivinerCardActions.Scry(this, choiceContext, IsUpgraded ? 6 : 4);
        if (DivinerCombatRuntime.HasDivinatedThisCombat)
        {
            await CardPileCmd.Draw(choiceContext, 1, Owner, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
