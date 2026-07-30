using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Common;

public class OmensAlign : DivinerCard
{
    public OmensAlign()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Omens Align",
        "Put all Fortune and Misfortune cards from anywhere into your hand.",
        "征兆相合",
        "将所有位置的福运和噩运牌加入你的手牌。",
        ("upgradedDesc", "Put all Fortune and Misfortune cards from anywhere into your hand.", "将所有位置的福运和噩运牌加入你的手牌。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var generatedOmens = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Concat(PileType.Exhaust.GetPile(Owner).Cards)
            .Where(card => card is Fortune or Misfortune)
            .ToList();
        if (generatedOmens.Count == 0)
        {
            return;
        }

        await CardPileCmd.Add(generatedOmens, PileType.Hand, CardPilePosition.Bottom, this, false);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
