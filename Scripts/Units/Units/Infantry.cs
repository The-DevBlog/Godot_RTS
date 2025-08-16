using Godot;
using MyEnums;

public partial class Infantry : Unit
{
	[Export] public InfantryType InfantryType { get; set; }

	public override void _Ready()
	{
		base._Ready();

		if (InfantryType == InfantryType.None) Utils.PrintErr("InfantryType is not set for infantry");
	}
}
