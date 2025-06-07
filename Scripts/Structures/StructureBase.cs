using Godot;

public partial class StructureBase : StaticBody3D
{
	[Export] public int Energy { get; set; }
	[Export] public int Cost { get; set; }
	[Export] public int BuildTime { get; set; }

	public override void _Ready()
	{
		if (Energy == 0)
			Utils.PrintErr("No Energy Assigned to structure");

		if (Cost == 0)
			Utils.PrintErr("No Cost Assigned to structure");

		if (BuildTime == 0)
			Utils.PrintErr("No BuildTime Assigned to structure");
	}
}
