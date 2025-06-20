using Godot;

public partial class SceneResources : Node3D
{
	public static SceneResources Instance { get; set; }
	[Export] public Vector2 MapSize { get; set; }
	[Export] public int Funds { get; set; }
	public int Energy { get; set; }
	public int EnergyConsumed { get; set; }

	public SceneResources()
	{
		Instance = this;

		if (Funds == 0)
			Utils.PrintErr("No Funds Assigned");
	}
}
