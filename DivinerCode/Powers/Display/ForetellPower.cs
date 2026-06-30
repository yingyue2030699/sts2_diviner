using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using Diviner.DivinerCode.Localization;
using Diviner.DivinerCode.Mechanics;
using MegaCrit.Sts2.Core.Localization;

namespace Diviner.DivinerCode.Powers.Display;

public class ForetellPower : DivinerDisplayPower, IAddDumbVariablesToPowerDescription
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Foretell",
        "Queued Foretell effects resolve at the start of your next turn.\nNext: {ForetellDetails}",
        "Queued Foretell effects resolve at the start of your next turn.\nNext: {ForetellDetails}",
        "预言",
        "已排队的预言效果会在你下个回合开始时结算。\n即将结算：{ForetellDetails}",
        "已排队的预言效果会在你下个回合开始时结算。\n即将结算：{ForetellDetails}"
    );

    public void AddDumbVariablesToPowerDescription(LocString description)
    {
        description.Add("ForetellDetails", DivinerCombatRuntime.ForetellDetailSummary);
    }
}
