using Godot;
using MyEnums;

public partial class Vehicle : Unit
{
	[Export] public VehicleType VehicleType { get; set; }

	public override void _Ready()
	{
		base._Ready();

		if (VehicleType == VehicleType.None) Utils.PrintErr("VehicleType is not set for vehicle");
	}
}
