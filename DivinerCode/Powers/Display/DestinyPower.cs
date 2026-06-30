using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;

namespace Diviner.DivinerCode.Powers.Display;

public class DestinyPower : DivinerDisplayPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Destiny",
        "Current Destiny. 0 is Doomed; 5 is Revelation.",
        "Current Destiny. 0 is Doomed; 5 is Revelation.",
        "命运",
        "当前命运。0 为劫兆；5 为启示。",
        "当前命运。0 为劫兆；5 为启示。"
    );
}
