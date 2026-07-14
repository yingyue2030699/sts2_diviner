using BaseLib.Abstracts;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Diviner.DivinerCode.Cards.Common;

public class WardSign : DivinerCard
{
    public WardSign()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithBlock(9, 3);
        WithDivinerKeywordTips(DivinerKeywords.GoodOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Ward Sign",
        "Gain !Block! Block. Good Omen: Retain a card in your hand this turn.",
        "守护符记",
        "获得 !Block! 点格挡。吉兆：使你手牌中的一张牌在本回合保留。",
        ("selectPrompt", "Choose a card to Retain this turn.", "选择一张本回合保留的牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        if (!DestinyService.IsGoodOmen())
        {
            return;
        }

        bool hasSelectableCard = PileType.Hand.GetPile(Owner).Cards.Any(card => !ReferenceEquals(card, this));
        if (!hasSelectableCard)
        {
            return;
        }

        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectPrompt"), 1, 1),
            card => !ReferenceEquals(card, this),
            this
        );
        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard != null)
        {
            DivinerCombatRuntime.MarkCardRetainThisTurn(selectedCard);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
