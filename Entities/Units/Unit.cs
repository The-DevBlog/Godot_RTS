using Godot;

public partial class Unit : Node3D
{
	private MeshInstance3D _meshInstance3D;
	private Sprite3D _selectBorder;
	private bool _selected = false;
	public bool Selected
	{
		get => _selected;
		set
		{
			if (_selected == value)
				return;

			_selected = value;
			UpdateMaterial();
		}
	}

	public override void _Ready()
	{
		_meshInstance3D = GetNode<MeshInstance3D>("MeshInstance3D");
		_selectBorder = GetNode<Sprite3D>("SelectBorder");
		_selectBorder.Visible = false;
	}

	// Updates the materials based on the selection state.
	private void UpdateMaterial()
	{
		_selectBorder.Visible = _selected;
	}
}
