using Godot;

public partial class Materials : Node
{
	public static Materials Instance { get; private set; }
	public StandardMaterial3D Selected { get; set; }
	public StandardMaterial3D Unselected { get; set; }

	public override void _Ready()
	{
		Instance = this;

		Selected = GD.Load<StandardMaterial3D>("res://Assets/Materials/Selected.tres");
		Unselected = GD.Load<StandardMaterial3D>("res://Assets/Materials/Unselected.tres");
	}
}
