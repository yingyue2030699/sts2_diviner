using MegaCrit.Sts2.Core.Entities.Powers;

namespace Diviner.DivinerCode.Powers.Display;

public abstract class DivinerDisplayPower : DivinerPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
