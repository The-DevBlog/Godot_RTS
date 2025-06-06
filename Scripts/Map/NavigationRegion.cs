using Godot;

public partial class NavigationRegion : NavigationRegion3D
{
	private Signals _signals;

	public override void _Ready()
	{
		_signals = Signals.Instance;
		_signals.UpdateNavigationMap += OnUpdateNavigationMap;
	}

	private void OnUpdateNavigationMap(NavigationRegion3D region)
	{
		if (region != this)
			return;

		GD.Print("Re-baking Navigation Mesh");
		BakeNavigationMesh();
	}
}
