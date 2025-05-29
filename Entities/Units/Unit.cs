using Godot;

public partial class Unit : RigidBody3D
{
	[Export]
	public int Speed { get; set; }
	[Export]
	public int Acceleration { get; set; }

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

	public override void _PhysicsProcess(double delta)
	{
		MoveUnit(delta);
	}

	public override void _Ready()
	{
		_navigationAgent = new NavigationAgent3D();
		_selectBorder = GetNode<Sprite3D>("SelectBorder");
		_selectBorder.Visible = false;
		_targetPosition = Vector3.Zero;
		Signals.Instance.SetTargetPosition += HandleSetTargetPosition;
	}

	private void MoveUnit(double delta)
	{
		if (_navigationAgent == null || !_selected)
			return;

		Vector3 direction = _navigationAgent.GetNextPathPosition() - this.GlobalPosition;
		direction = direction.Normalized();

		LinearVelocity = LinearVelocity.Lerp(direction * Speed, Acceleration * (float)delta);
	}

	private void HandleSetTargetPosition(Vector3 targetPosition)
	{
		if (!_selected)
			return;

		_navigationAgent.TargetPosition = targetPosition;

		// direction = _navigationAgent.GetNextPathPosition() - this.GlobalPosition;
		// direction = direction.Normalized();

		// LinearVelocity = LinearVelocity.Lerp()
	}

	// Updates the materials based on the selection state.
	private void UpdateMaterial()
	{
		_selectBorder.Visible = _selected;
	}
}
