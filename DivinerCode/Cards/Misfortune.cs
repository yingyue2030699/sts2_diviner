using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Powers.CardPowers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Cards;

public class Misfortune : DivinerCard
{
    public Misfortune()
        : base(3, CardType.Attack, CardRarity.Basic, TargetType.AllEnemies)
    {
        WithDamage(15, 0);
        WithKeywords([CardKeyword.Exhaust]);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Misfortune",
        "Deal !Damage! damage to all enemies. At end of turn, lose 5 HP and trigger this effect.",
        "厄运",
        "对所有敌人造成 !Damage! 点伤害。回合结束时，失去 5 点生命并触发此效果。"
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ResolveMisfortune(choiceContext, false);
    }

    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await ResolveMisfortune(choiceContext, true);
    }

    protected override void OnUpgrade()
    {
    }

    private async Task ResolveMisfortune(PlayerChoiceContext choiceContext, bool fromAutoplay)
    {
        if (fromAutoplay)
        {
            int hpLoss = DoomEnginePower.GetHpLoss(Owner, 5);
            if (hpLoss > 0)
            {
                await CreatureCmd.Damage(choiceContext, Owner.Creature, hpLoss, DamageProps.nonCardHpLoss, Owner.Creature, this);
            }
        }

        var enemies = CombatState?.HittableEnemies.Where(creature => creature.Side != Owner.Creature.Side).ToList() ?? [];
        if (enemies.Count > 0)
        {
            await CreatureCmd.Damage(choiceContext, enemies, 15 + DoomEnginePower.GetDamageBonus(Owner), DamageProps.nonCardUnpowered, Owner.Creature, this);
        }
    }
}
