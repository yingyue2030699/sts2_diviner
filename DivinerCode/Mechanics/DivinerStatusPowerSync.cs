using Diviner.DivinerCode.Powers.CardPowers;
using Diviner.DivinerCode.Powers.Display;
using Diviner.DivinerCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Diviner.DivinerCode.Mechanics;

public static class DivinerStatusPowerSync
{
    public static async Task Sync(Player? player, PlayerChoiceContext? choiceContext = null)
    {
        if (player?.Creature == null)
        {
            return;
        }

        DivinerCombatRuntime.TrackPlayer(player);
        await DivinerRelicHooks.OnStatusSync(player, choiceContext);
        if (choiceContext != null)
        {
            int ledgerBlock = DivinerCombatRuntime.ConsumePendingLedgerBlock(player);
            if (ledgerBlock > 0)
            {
                await CreatureCmd.GainBlock(
                    player.Creature,
                    ledgerBlock,
                    BlockProps.cardUnpowered,
                    null,
                    true);
            }
        }

        await SyncPower<DestinyPower>(player.Creature, DestinyService.CurrentDestiny, choiceContext);
        await SyncPower<ForetellPower>(player.Creature, DivinerCombatRuntime.QueuedForetellCount, choiceContext);
        await SyncPower<SmokeAndMirrorsPower>(
            player.Creature,
            DivinerCombatRuntime.NextForetellDamageOrBlockBonus,
            choiceContext);
    }

    public static async Task Clear(Creature? creature)
    {
        if (creature == null)
        {
            return;
        }

        await PowerCmd.Remove<DestinyPower>(creature);
        await PowerCmd.Remove<ForetellPower>(creature);
        await PowerCmd.Remove<SmokeAndMirrorsPower>(creature);
    }

    private static async Task SyncPower<TPower>(
        Creature owner,
        int amount,
        PlayerChoiceContext? choiceContext) where TPower : PowerModel
    {
        var existing = owner.GetPower<TPower>();
        if (amount <= 0)
        {
            if (existing != null)
            {
                await PowerCmd.Remove<TPower>(owner);
            }

            return;
        }

        if (existing == null)
        {
            if (choiceContext == null)
            {
                return;
            }

            await PowerCmd.Apply<TPower>(choiceContext, owner, amount, owner, null!, true);
            return;
        }

        var delta = amount - existing.Amount;
        if (delta == 0)
        {
            return;
        }

        if (choiceContext == null)
        {
            return;
        }

        await PowerCmd.ModifyAmount(choiceContext, existing, delta, owner, null!, true);
    }
}
