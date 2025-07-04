using Godot;
using MyEnums;

public partial class Unit : CharacterBody3D, ICostProvider
{
	[Export] public int Speed { get; set; }
	[Export] public int HP { get; set; }
	[Export] public int DPS { get; set; }
	[Export] public int Cost { get; set; }
	[Export] public int BuildTime { get; set; }
	[Export] public int Acceleration { get; set; }
	[Export] public bool DebugEnabled { get; set; }
	[Export] private Node3D _healthbar;
	public Player Player { get; set; }
	private float _movementDelta;
	private Vector3 _targetPosition;
	private NavigationAgent3D _navigationAgent;
	private Sprite3D _selectBorder;
	private bool _selected = false;
	private Camera3D _cam;
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
		_cam = GetViewport().GetCamera3D();

		if (HP == 0) Utils.PrintErr("No HP Assigned to unit");
		if (DPS == 0) Utils.PrintErr("No DPS Assigned to unit");
		if (Speed == 0) Utils.PrintErr("No Speed Assigned to unit");
		if (Cost == 0) Utils.PrintErr("No Cost Assigned to unit");
		if (BuildTime == 0) Utils.PrintErr("No BuildTime Assigned to unit");
		if (Acceleration == 0) Utils.PrintErr("No Acceleration Assigned to unit");
		Utils.NullExportCheck(_healthbar);
	}

	public override void _Process(double delta)
	{
		// ScaleHealthbar();
	}

	public override void _PhysicsProcess(double delta)
	{
		MoveUnit();
	}

	// scales the healthbar based on distance to camera
	private void ScaleHealthbar()
	{
		float desiredHeightPx = 32f;

		// 1) distance
		float d = GlobalPosition.DistanceTo(_cam.GlobalPosition);
		// 2) vertical FOV in radians
		float vfov = _cam.Fov * (Mathf.Pi / 180f);
		// 3) voxels per pixel
		float viewportH = GetViewport().GetVisibleRect().Size.Y;
		float worldPerPx = 2f * d * Mathf.Tan(vfov * 0.5f) / viewportH;
		// 4) apply only to Y
		_healthbar.Scale = new Vector3(
			_healthbar.Scale.X,
			desiredHeightPx * worldPerPx,
			_healthbar.Scale.Z
		);
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
