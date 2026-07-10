using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Diviner.DivinerCode.Character;
using Diviner.DivinerCode.Extensions;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Diviner.DivinerCode.Potions;

[Pool(typeof(DivinerPotionPool))]
public abstract class DivinerPotion : CustomPotionModel
{
    private string ImageSlug => $"{BuildImageSlug()}.png";

    public override TargetType TargetType => TargetType.Self;

    public override string CustomPackedImagePath => ImageSlug.PotionImagePath();

    public override string CustomPackedOutlinePath => $"{BuildImageSlug()}_outline.png".PotionImagePath();

    protected Player? ResolvePlayer(Creature? target)
    {
        var player = target?.Player ?? Owner ?? DivinerCombatRuntime.GetLastObservedPlayer();
        DivinerCombatRuntime.TrackPlayer(player);
        return player;
    }

    private string BuildImageSlug()
    {
        var raw = Id.Entry.RemovePrefix();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "potion";
        }

        return raw.Replace("-", "_", StringComparison.Ordinal).ToLowerInvariant();
    }
}
