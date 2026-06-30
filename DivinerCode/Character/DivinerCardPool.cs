using BaseLib.Abstracts;
using Diviner.DivinerCode.Extensions;
using Godot;

namespace Diviner.DivinerCode.Character;

public class DivinerCardPool : CustomCardPoolModel
{
    public override string Title => Diviner.CharacterId;

    public override float H => 0.73f;
    public override float S => 0.28f;
    public override float V => 1.02f;

    public override Color DeckEntryCardColor => Diviner.Color;

    public override string? BigEnergyIconPath => "big_energy.png".CharacterUiPath();

    public override string? TextEnergyIconPath => "text_energy.png".CharacterUiPath();

    public override bool IsColorless => false;
}
