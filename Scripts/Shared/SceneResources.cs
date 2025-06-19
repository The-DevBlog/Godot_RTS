using Godot;

public partial class SceneResources : Node3D
{
	public static SceneResources Instance { get; set; }
	[Export] public Vector2 MapSize { get; set; }

	public SceneResources()
	{
		Instance = this;
	}
}
