using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Diviner.DivinerCode.Cards.Uncommon;

public class Epiphany : DivinerCard
{
    public Epiphany()
        : base(4, CardType.Skill, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithKeywords([CardKeyword.Exhaust]);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Epiphany",
        "Gain 1 Destiny.",
        "顿悟",
        "获得 1 点命运。",
        ("upgradedDesc", "Gain 1 Destiny.", "获得 1 点命运。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        DestinyService.AddDestiny(1);
        DestinyService.PersistCurrentState(Owner.RunState);
        await DivinerStatusPowerSync.Sync(Owner, choiceContext);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

public class ForetoldFalter : DivinerCard
{
    public ForetoldFalter()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithCostUpgradeBy(-1);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Foretold Falter",
        "Enemies with more than 10 Weak deal half damage to you.",
        "预示踉跄",
        "拥有超过 10 层虚弱的敌人对你造成的伤害减半。",
        ("upgradedDesc", "Enemies with more than 10 Weak deal half damage to you.", "拥有超过 10 层虚弱的敌人对你造成的伤害减半。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ForetoldFalterPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}

public class WeaveTheAegis : DivinerCard
{
    public WeaveTheAegis()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.TargetedNoCreature)
    {
        WithBlock(12, 4);
        WithDivinerKeywordTips(DivinerKeywords.Destiny);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Weave the Aegis",
        "Whenever your Destiny changes, gain !Block! Block.",
        "织成神盾",
        "每当你的命运变化，获得 !Block! 点格挡。",
        ("upgradedDesc", "Whenever your Destiny changes, gain !Block! Block.", "每当你的命运变化，获得 !Block! 点格挡。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WeaveTheAegisPower>(choiceContext, Owner.Creature, IsUpgraded ? 16 : 12, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
    }
}
