using Godot;

public partial class SceneResources : Node3D
{
	public static SceneResources Instance { get; set; }
	[Export] public Vector2 MapSize { get; set; }
	[Export] public int Funds { get; set; }
	[Export] public Color TeamColor;
	[Export] public bool RainyWeather { get; set; }
	public int Energy { get; set; }
	public int EnergyConsumed { get; set; }

	public override void _EnterTree()
	{
		base._EnterTree();  // **must** call this first
		Instance = this;

		if (Funds == 0)
			Utils.PrintErr("No Funds Assigned");
	}
}
