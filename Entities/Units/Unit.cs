using Godot;

public partial class Unit : Node3D
{
	private MeshInstance3D _meshInstance3D;
	private bool _selected = false;
	public bool Selected
	{
		get => _selected;
		set
		{
			if (_selected == value)
				return;

			_selected = value;
			OnSelectionChanged();
		}
	}

	public override void _Ready()
	{
		_meshInstance3D = GetNode<MeshInstance3D>("MeshInstance3D");
	}

	private void OnSelectionChanged()
	{
		if (_selected)
			_meshInstance3D.MaterialOverride = AssetServer.Instance.Materials.Selected;
		else
			_meshInstance3D.MaterialOverride = AssetServer.Instance.Materials.Unselected;
	}
}
