using Godot;
using Name;

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
		AddToGroup(Groups.Units.ToString());
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
		MoveUnit(delta);
	}

	private void MoveUnit(double delta)
	{
		if (NavigationServer3D.MapGetIterationId(_navigationAgent.GetNavigationMap()) == 0)
			return;

		if (_navigationAgent.IsNavigationFinished())
			return;

		// _movementDelta = Speed * (float)delta;
		// Vector3 nextPathPosition = _navigationAgent.GetNextPathPosition();
		// Vector3 newVelocity = GlobalPosition.DirectionTo(nextPathPosition) * _movementDelta;
		// if (_navigationAgent.AvoidanceEnabled)
		// {
		// 	_navigationAgent.Velocity = newVelocity;
		// }
		// else
		// {
		// 	OnVelocityComputed(newVelocity);
		// }

		Vector3 nextPathPosition = _navigationAgent.GetNextPathPosition();
		Vector3 newVelocity = GlobalPosition.DirectionTo(nextPathPosition) * Speed;
		if (_navigationAgent.AvoidanceEnabled)
		{
			_navigationAgent.Velocity = newVelocity;
		}
		else
		{
			OnVelocityComputed(newVelocity);
		}

		LookAt(nextPathPosition, Vector3.Up);
	}

	private void OnVelocityComputed(Vector3 safeVelocity)
	{
		// GlobalPosition = GlobalPosition.MoveToward(GlobalPosition + safeVelocity, _movementDelta);
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
