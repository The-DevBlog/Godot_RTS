using Godot;

public partial class NavigationRegion : NavigationRegion3D
{
	private MeshInstance3D _groundMesh;
	private CollisionShape3D _groundCollider;
	private Signals _signals;
	private GlobalResources _globalResources;

	public override void _Ready()
	{
		_globalResources = GlobalResources.Instance;
		_signals = Signals.Instance;
		_signals.UpdateNavigationMap += OnUpdateNavigationMap;
		_groundMesh = _globalResources.Map.GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
		_groundCollider = _globalResources.Map.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

		Utils.NullCheck(_groundMesh);
		Utils.NullCheck(_groundCollider);

		ResizeGroundToMapSize();
		BakeNavigationMesh();
	}

	private void ResizeGroundToMapSize()
	{
		Vector2 mapSize = GlobalResources.Instance.MapSize;
		GD.Print("Resizing ground to map size " + mapSize);

		// Resize PlaneMesh (only works if GroundMesh.Mesh is a PlaneMesh)
		if (_groundMesh.Mesh is BoxMesh plane)
			plane.Size = new Vector3(mapSize.X, 0.5f, mapSize.Y);
		else
			Utils.PrintErr("GroundMesh.Mesh is not a PlaneMesh. Cannot set size directly.");

		// Resize GroundCollider (only works if it's a BoxShape3D)
		if (_groundCollider.Shape is BoxShape3D box)
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
