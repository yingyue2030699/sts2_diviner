using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Diviner.DivinerCode.Mechanics;

public static class DivinerEffectCue
{
    public static void Divinate(Creature? creature)
    {
        Play(creature, VfxCmd.starryImpactVfx, "magic_cast.mp3");
    }

    public static void DestinyIncrease(Creature? creature)
    {
        Play(creature, VfxCmd.healPath, "buff.mp3");
    }

    public static void DestinyDecrease(Creature? creature)
    {
        Play(creature, VfxCmd.spookyScreamVfx, "debuff.mp3");
    }

    public static void Revelation(Creature? creature)
    {
        Play(creature, VfxCmd.lightningPath, "lightning.mp3");
    }

    public static void DoomedCountdownBell()
    {
        Safe(() => SfxCmd.Play("bell.mp3", 1f));
        Safe(() => SfxCmd.Play("bell.mp3", 0.82f));
        Safe(() => SfxCmd.Play("debuff.mp3", 0.55f));
    }

    public static void BombardmentImpact(IEnumerable<Creature> targets)
    {
        var targetList = targets.ToList();
        if (targetList.Count == 0)
        {
            return;
        }

        Safe(() => VfxCmd.PlayOnCreatureCenters(targetList, VfxCmd.bluntPath));
        Safe(() => SfxCmd.Play("blunt_attack.mp3", 1f));
    }

    private static void Play(Creature? creature, string vfx, string sfx)
    {
        if (creature != null)
        {
            Safe(() => VfxCmd.PlayOnCreatureCenter(creature, vfx));
        }

        Safe(() => SfxCmd.Play(sfx, 0.8f));
    }

    private static void Safe(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"Diviner effect cue failed: {ex.Message}");
        }
    }
}
