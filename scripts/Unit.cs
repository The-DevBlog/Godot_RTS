using Godot;

public partial class Unit : Node3D
{
	private MeshInstance3D _meshInstance3D;
	private Color _normalColor = new Color(0.0f, 0.65f, 0.2f, 1.0f);
	private Color _selectedColor = new Color(0.0f, 0.55f, 0.6f, 1.0f);
	private bool _selected = false;
	[Export] public Material UnselectedMaterial;
	[Export] public Material SelectedMaterial;
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
		_meshInstance3D = GetNode<MeshInstance3D>("RigidBody3D/MeshInstance3D");
	}


	private void OnSelectionChanged()
	{
		if (_selected)
			_meshInstance3D.SetSurfaceOverrideMaterial(0, SelectedMaterial);
		else
			_meshInstance3D.SetSurfaceOverrideMaterial(0, UnselectedMaterial);
	}
}
