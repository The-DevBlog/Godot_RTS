using Godot;
using Name;

public partial class Unit : CharacterBody3D
{
	[Export]
	public int Speed { get; set; } = 10;
	[Export]
	public int Acceleration { get; set; } = 3;
	[Export]
	public bool DebugEnabled { get; set; }

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

		Vector3 nextPathPosition = _navigationAgent.GetNextPathPosition();
		Vector3 newVelocity = GlobalPosition.DirectionTo(nextPathPosition) * Speed;

		Velocity = newVelocity;

		LookAt(nextPathPosition, Vector3.Up);
		MoveAndSlide();
	}

	private void HandleSetTargetPosition(Vector3 targetPosition)
	{
		if (!_selected)
			return;

		SetMoveTarget(targetPosition);
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
