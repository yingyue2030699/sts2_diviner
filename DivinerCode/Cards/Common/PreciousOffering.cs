using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class PreciousOffering : DivinerCard
{
    public PreciousOffering()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithDivinerKeywordTips(DivinerKeywords.Divinate);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Precious Offering",
        "Exhaust a card in your hand. If a Rare card is Exhausted, Divinate.",
        "珍贵献礼",
        "消耗一张手牌。如果消耗的是稀有牌，占卜。",
        ("upgradedDesc", "Exhaust a card in your hand. If an Uncommon or Rare card is Exhausted, Divinate.", "消耗一张手牌。如果消耗的是罕见或稀有牌，占卜。"),
        ("selectPrompt", "Choose a card to Exhaust.", "选择一张牌消耗。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectedCard = (await DivinerCardActions.SelectFromHand(this, choiceContext, "selectPrompt", 1, 1)).FirstOrDefault();
        if (selectedCard == null)
        {
            return;
        }

        bool shouldDivinate = selectedCard.Rarity == CardRarity.Rare ||
                              (IsUpgraded && selectedCard.Rarity == CardRarity.Uncommon);
        await CardCmd.Exhaust(choiceContext, selectedCard, false, false);
        if (shouldDivinate)
        {
            await DivinationService.RecordPlaceholder(choiceContext, Owner, "Precious Offering");
        }
    }

    protected override void OnUpgrade()
    {
    }
}
