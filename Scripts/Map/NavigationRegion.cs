using Godot;

public partial class NavigationRegion : NavigationRegion3D
{
	[Export] public MeshInstance3D GroundMesh { get; set; }
	[Export] public CollisionShape3D GroundCollider { get; set; }

	private Signals _signals;

	public override void _Ready()
	{
		_signals = Signals.Instance;
		_signals.UpdateNavigationMap += OnUpdateNavigationMap;

		Utils.NullExportCheck(GroundMesh);
		Utils.NullExportCheck(GroundCollider);

		ResizeGroundToMapSize();
	}

	private void ResizeGroundToMapSize()
	{
		Vector2 mapSize = Resources.Instance.MapSize;

		// Resize PlaneMesh (only works if GroundMesh.Mesh is a PlaneMesh)
		if (GroundMesh.Mesh is BoxMesh plane)
			plane.Size = new Vector3(mapSize.X, 0.5f, mapSize.Y);
		else
			Utils.PrintErr("GroundMesh.Mesh is not a PlaneMesh. Cannot set size directly.");

		// Resize GroundCollider (only works if it's a BoxShape3D)
		if (GroundCollider.Shape is BoxShape3D box)
			box.Size = new Vector3(mapSize.X, 0.5f, mapSize.Y);
		else
			Utils.PrintErr("GroundCollider.Shape is not a BoxShape3D.");
	}


	private void OnUpdateNavigationMap(NavigationRegion3D region)
	{
		if (region != this)
			return;

		BakeNavigationMesh();
		GD.Print("Re-baking Navigation Mesh");
	}
}
