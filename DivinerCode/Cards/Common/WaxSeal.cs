using BaseLib.Abstracts;
using BaseLib.Commands;
using BaseLib.Utils;
using Diviner.DivinerCode.Localization;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Diviner.DivinerCode.Cards.Common;

public class WaxSeal : DivinerCard
{
    public WaxSeal()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(9, 3);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Wax Seal",
        "Deal !Damage! damage. Put a card from your draw or discard pile on top of your draw pile.",
        "蜡封",
        "造成 !Damage! 点伤害。将一张抽牌堆或弃牌堆中的牌放到抽牌堆顶。",
        ("selectPrompt", "Choose a card to put on top of your draw pile.", "选择一张牌放到抽牌堆顶。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        var selectableCards = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .ToList();
        if (selectableCards.Count == 0)
        {
            return;
        }

        var selected = await MultiPileCardSelect.Select(
            choiceContext,
            Owner,
            new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectPrompt"), 1, 1),
            selectableCards,
            [PileType.Draw, PileType.Discard]
        );
        var selectedCard = selected.FirstOrDefault();
        if (selectedCard != null)
        {
            await CardPileCmd.Add(selectedCard, PileType.Draw, CardPilePosition.Top, this, false);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
