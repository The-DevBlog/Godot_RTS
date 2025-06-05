using Godot;
using MyEnums;

public partial class Unit : CharacterBody3D
{
	[Export]
	public int Speed { get; set; } = 2;
	[Export]
	public int Acceleration { get; set; } = 3;
	[Export]
	public bool DebugEnabled { get; set; }

	private float _movementDelta;
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
			ToggleSelectBorder();
		}
	}

	public override void _Ready()
	{
		AddToGroup(Group.Units.ToString());
		_navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_navigationAgent.AvoidanceEnabled = true;
		_navigationAgent.DebugEnabled = DebugEnabled;
		_navigationAgent.VelocityComputed += OnVelocityComputed;

		_selectBorder = GetNode<Sprite3D>("SelectBorder");
		_selectBorder.Visible = false;

		_targetPosition = Vector3.Zero;
	}

	public override void _PhysicsProcess(double delta)
	{
		MoveUnit();
	}

	private void MoveUnit()
	{
		if (NavigationServer3D.MapGetIterationId(_navigationAgent.GetNavigationMap()) == 0)
			return;

		if (_navigationAgent.IsNavigationFinished())
			return;

		// 1) Ask the agent for the next waypoint:
		Vector3 nextPathPosition = _navigationAgent.GetNextPathPosition();

		// 2) Build a pure‐horizontal direction: 
		Vector3 toTarget = nextPathPosition - GlobalPosition;
		toTarget.Y = 0;                         // force Y = 0 so we don't “pop” upward
		if (toTarget.LengthSquared() < 0.001f)  // if we're basically on‐top of the waypoint, skip
			return;

		Vector3 horizontalDir = toTarget.Normalized();

		// 3) Multiply by speed to get a flat velocity
		Vector3 newVelocity = horizontalDir * Speed;

		// 4) Give that to the NavigationAgent (or directly to MoveAndSlide) 
		if (_navigationAgent.AvoidanceEnabled)
			_navigationAgent.Velocity = newVelocity;
		else
			OnVelocityComputed(newVelocity);

		// 5) Make the unit face the horizontal direction:
		LookAt(GlobalPosition + horizontalDir, Vector3.Up);
	}

	// private void MoveUnit()
	// {
	// 	if (NavigationServer3D.MapGetIterationId(_navigationAgent.GetNavigationMap()) == 0)
	// 		return;

	// 	if (_navigationAgent.IsNavigationFinished())
	// 		return;

	// 	Vector3 nextPathPosition = _navigationAgent.GetNextPathPosition();
	// 	Vector3 newVelocity = GlobalPosition.DirectionTo(nextPathPosition) * Speed;

	// 	if (_navigationAgent.AvoidanceEnabled)
	// 		_navigationAgent.Velocity = newVelocity;
	// 	else
	// 		OnVelocityComputed(newVelocity);

	// 	LookAt(nextPathPosition, Vector3.Up);
	// }

	private void OnVelocityComputed(Vector3 safeVelocity)
	{
		Velocity = safeVelocity;
		MoveAndSlide();
	}

	private void ToggleSelectBorder()
	{
		_selectBorder.Visible = _selected;
	}

	public void SetMoveTarget(Vector3 worldPos)
	{
		_targetPosition = worldPos;
		_navigationAgent.TargetPosition = worldPos;
	}
}
