using Godot;

public partial class Unit : Node3D
{
	[Export]
	public int Speed { get; set; }
	[Export] public int Acceleration { get; set; }

	private Vector3 _targetPosition;
	private NavigationAgent3D _navigationAgent;
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
		_navigationAgent = new NavigationAgent3D();
		_selectBorder = GetNode<Sprite3D>("SelectBorder");
		_selectBorder.Visible = false;
		_targetPosition = Vector3.Zero;
		Signals.Instance.SetTargetPosition += HandleSetTargetPosition;
	}

	private void HandleSetTargetPosition(Vector3 targetPosition)
	{
		if (!_selected)
			return;

		// grab camera and mouse pos
		var cam = GetViewport().GetCamera3D();
		Vector2 mousePos = GetViewport().GetMousePosition();

		// build a ray: mouse screen space to ground
		Vector3 from = cam.ProjectRayOrigin(mousePos);
		Vector3 dir = cam.ProjectRayNormal(mousePos);
		Vector3 to = from + dir * 1000f; // cast 1000 units out

		var spaceState = GetWorld3D().DirectSpaceState;
		var rayParams = new PhysicsRayQueryParameters3D()
		{
			From = from,
			To = to,
		};

		var result = spaceState.IntersectRay(rayParams);

		// assign new target position
		if (result.TryGetValue("position", out Variant hitPosVar))
		{
			Vector3 hitPos = hitPosVar.AsVector3();
			_targetPosition = hitPos;
			GD.Print($"Target position set to: {hitPos}");
		}
	}

	// Updates the materials based on the selection state.
	private void UpdateMaterial()
	{
		_selectBorder.Visible = _selected;
	}
}
