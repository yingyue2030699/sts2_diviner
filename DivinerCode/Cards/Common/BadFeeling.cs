using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Diviner.DivinerCode.Cards.Common;

public class BadFeeling : DivinerCard
{
    public BadFeeling()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDivinerKeywordTips(DivinerKeywords.BadOmen);
    }

    public override List<(string, string)>? Localization => DivinerLoc.Card(
        "Bad Feeling",
        "Apply 2 Weak. Bad Omen: apply 2 Vulnerable.",
        "不祥预感",
        "给予 2 层虚弱。凶兆：给予 2 层易伤。",
        ("upgradedDesc", "Apply 2 Weak to all enemies. Bad Omen: apply 2 Vulnerable to all enemies.", "对所有敌人给予 2 层虚弱。凶兆：对所有敌人给予 2 层易伤。")
    );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool shouldApplyVulnerable = DestinyService.IsBadOmen();

        if (IsUpgraded)
        {
            var enemies = CombatState?.HittableEnemies.Where(creature => creature.Side != Owner.Creature.Side).ToList() ?? [];
            foreach (var enemy in enemies)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, enemy, 2, Owner.Creature, this, false);
                if (shouldApplyVulnerable)
                {
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, 2, Owner.Creature, this, false);
                }
            }
        }
        else if (cardPlay.Target != null)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, 2, Owner.Creature, this, false);
            if (shouldApplyVulnerable)
            {
                await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, 2, Owner.Creature, this, false);
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}
