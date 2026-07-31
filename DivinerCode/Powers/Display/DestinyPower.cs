using BaseLib.Abstracts;
using Diviner.DivinerCode.Localization;

namespace Diviner.DivinerCode.Powers.Display;

public class DestinyPower : DivinerDisplayPower
{
    public override List<(string, string)>? Localization => DivinerLoc.Power(
        "Destiny",
        "Current Destiny. 0 is Doomed; 5 is Revelation. If you would lose Destiny while at 0, lose 1 Countdown of Destiny instead.",
        "Current Destiny. 0 is Doomed; 5 is Revelation. If you would lose Destiny while at 0, lose 1 Countdown of Destiny instead.",
        "命运",
        "当前命运。0 为劫兆；5 为启示。命运为 0 时，若将失去命运，则改为失去 1 层命运倒计时。",
        "当前命运。0 为劫兆；5 为启示。命运为 0 时，若将失去命运，则改为失去 1 层命运倒计时。"
    );
}
