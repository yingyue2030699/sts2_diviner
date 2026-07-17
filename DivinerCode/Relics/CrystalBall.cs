using BaseLib.Abstracts;
using Diviner.DivinerCode.Cards;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Diviner.DivinerCode.Relics;

public class CrystalBall : DivinerRelic
{
    private ICombatState? _lastCombatState;
    private bool _appliedStartOfCombatEffects;
    private bool _addedStartOfCombatCard;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Crystal Ball",
        "At the start of combat, add a Fortune or Misfortune to your hand. Right-click to open or close the Destiny ledger.",
        "A cloudy lens full of futures that almost happened.",
        "水晶球",
        "战斗开始时，将一张福运或厄运加入你的手牌。右键点击以打开或关闭命运账册。",
        "朦胧的镜面中满是差点成真的未来。"
    );

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState
    )
    {
        if (!ReferenceEquals(_lastCombatState, combatState))
        {
            _lastCombatState = combatState;
            _appliedStartOfCombatEffects = false;
            _addedStartOfCombatCard = false;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(player, Owner))
        {
            return;
        }

        DivinerCombatRuntime.ClearTemporaryRetainCards(Owner);
        DivinerCombatRuntime.PlayCriticalDoomedWarningIfNeeded(Owner);

        if (!_appliedStartOfCombatEffects)
        {
            _appliedStartOfCombatEffects = true;
            Flash();
            DestinyService.EnsureLoadedForPlayer(Owner);
            await DivinerCombatRuntime.TryBeginStartOfCombatDestinyEffects(choiceContext, Owner, this);
            await DivinerStatusPowerSync.Sync(Owner, choiceContext);
        }

        if (_addedStartOfCombatCard)
        {
            return;
        }

        _addedStartOfCombatCard = true;
        if (_lastCombatState == null)
        {
            MainFile.Logger.Error("Crystal Ball could not create Fortune/Misfortune because no combat state was tracked.");
            return;
        }

        await AddStartOfCombatCards();
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants
    )
    {
        await DivinerCombatRuntime.TickDredgeCountdownAtTurnEnd(
            choiceContext,
            side,
            participants,
            Owner,
            Owner.Creature
        );
    }

    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (!DivinerCombatRuntime.IsCardRetainedThisTurn(card))
        {
            return false;
        }

        keywords.Add(CardKeyword.Retain);
        return true;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (ReferenceEquals(cardPlay.Card.Owner, Owner))
        {
            DivinerCombatRuntime.ConsumeForcedFullOmen(Owner);
        }

        return Task.CompletedTask;
    }

    protected virtual async Task AddStartOfCombatCards()
    {
        if (_lastCombatState == null)
        {
            return;
        }

        var createdCard = _lastCombatState.CreateCard(ChooseStartOfCombatCard(Owner), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(createdCard, PileType.Hand, Owner, CardPilePosition.Bottom);
    }

    protected CardModel CreateStartOfCombatCard<TCard>(bool upgraded, bool freeThisTurn)
        where TCard : CardModel
    {
        if (_lastCombatState == null)
        {
            throw new InvalidOperationException("Crystal Ball has no combat state for generated start-of-combat cards.");
        }

        var createdCard = _lastCombatState.CreateCard(ModelDb.Card<TCard>(), Owner);
        if (upgraded)
        {
            createdCard.UpgradeInternal();
        }

        if (freeThisTurn)
        {
            createdCard.SetToFreeThisTurn();
        }

        return createdCard;
    }

    protected static MegaCrit.Sts2.Core.Models.CardModel ChooseStartOfCombatCard(Player player)
    {
        return DestinyService.IsBadOmen(player)
            ? ModelDb.Card<Misfortune>()
            : ModelDb.Card<Fortune>();
    }
}

public class DestinedCrystalBall : CrystalBall
{
    public override List<(string, string)>? Localization => DivinerLoc.Relic(
        "Destined Crystal Ball",
        "Your divinations are recorded here. At the start of combat, add a Fortune+ and a Misfortune+ to your hand. They cost 0 this turn.",
        "A clear lens: one blessing, one ruin, both already chosen.",
        "既定水晶球",
        "你的占卜记录于此。战斗开始时，将一张福运+和一张厄运+加入你的手牌。它们本回合费用为 0。",
        "清澈的镜面：一份祝福，一场毁灭，皆已注定。"
    );

    protected override async Task AddStartOfCombatCards()
    {
        var fortune = CreateStartOfCombatCard<Fortune>(true, true);
        var misfortune = CreateStartOfCombatCard<Misfortune>(true, true);
        await CardPileCmd.AddGeneratedCardsToCombat(
            [fortune, misfortune],
            PileType.Hand,
            Owner,
            CardPilePosition.Bottom);
    }
}
