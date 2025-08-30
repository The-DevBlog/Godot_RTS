using Godot;
using MyEnums;

public partial class Unit : CharacterBody3D, ICostProvider, IDamageable
{
	[Export] public int Team { get; set; }
	[Export] public int Speed { get; set; }
	[Export] public int HP { get; set; }
	[Export] public int DPS { get; set; }
	[Export] public int Range { get; set; }
	[Export] public float FireRate { get; set; }
	[Export] public int Cost { get; set; }
	[Export] public int BuildTime { get; set; }
	[Export] public int Acceleration { get; set; }
	[Export] public bool DebugEnabled { get; set; }
	[Export] public float ProjectileSpeed { get; set; }
	[Export] public Node3D Death;
	[Export] public float BulletSpread { get; set; }
	[Export] public WeaponType WeaponType { get; set; }
	[Export] private CombatSystem _combatSystem;
	[Export] private HealthSystem _healthSystem;
	[Export] private float _rotationSpeed = 220f;   // try 1000–2000 for tanks
	private Node3D _model;
	private float _facingWindowDeg = 10f; // start moving when |diff| <= this
	private float _stopWindowDeg = 18f;   // stop moving when |diff| > this (hysteresis)
	private bool _meshFacesPlusZ = false; // set false if your mesh faces -Z, eg: travel backwards or forwards

	private bool _moving = false; // hysteresis state
	public int CurrentHP { get; set; }
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
		AddToGroup(Group.units.ToString());

		_navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		Utils.NullExportCheck(_navigationAgent);
		_navigationAgent.AvoidanceEnabled = true;
		_navigationAgent.DebugEnabled = DebugEnabled;
		_navigationAgent.VelocityComputed += OnVelocityComputed;

		_selectBorder = GetNode<Sprite3D>("SelectBorder");
		_selectBorder.Visible = false;

		_model = GetNode<Node3D>("Model");
		_targetPosition = Vector3.Zero;
		_cam = GetViewport().GetCamera3D();

		if (HP == 0) Utils.PrintErr("No HP Assigned to unit");
		if (DPS == 0) Utils.PrintErr("No DPS Assigned to unit");
		if (Range == 0) Utils.PrintErr("No Range Assigned to unit");
		if (FireRate == 0) Utils.PrintErr("No FireRate Assigned to unit");
		if (Speed == 0) Utils.PrintErr("No Speed Assigned to unit");
		if (Cost == 0) Utils.PrintErr("No Cost Assigned to unit");
		if (BuildTime == 0) Utils.PrintErr("No BuildTime Assigned to unit");
		if (Acceleration == 0) Utils.PrintErr("No Acceleration Assigned to unit");
		if (Team == 0) Utils.PrintErr("No Team Assigned to unit");
		if (ProjectileSpeed == 0) Utils.PrintErr("No ProjectileSpeed Assigned to unit");
		if (BulletSpread == 0) Utils.PrintErr("No BulletSpread Assigned to unit");
		if (WeaponType == WeaponType.None) Utils.PrintErr("No WeaponType Assigned to unit");

		Utils.NullExportCheck(_combatSystem);
		Utils.NullExportCheck(_healthSystem);
		Utils.NullExportCheck(Death);
		Utils.NullCheck(_model);

		GD.Print("Built vehicle on team " + Team);

		CurrentHP = HP;

		SetTeamColor(_model, PlayerManager.Instance.HumanPlayer.Color);
	}

	public override void _PhysicsProcess(double delta) => MoveUnit(delta);

	public void ApplyDamage(int amount, Vector3 hitPos, Vector3 hitNormal)
	{
		_healthSystem.ApplyDamage(amount, hitPos, hitNormal);
	}

	public void SetMoveTarget(Vector3 worldPos)
	{
		_targetPosition = worldPos;
		_navigationAgent.TargetPosition = worldPos;
	}

	private void MoveUnit(double delta)
	{
		if (NavigationServer3D.MapGetIterationId(_navigationAgent.GetNavigationMap()) == 0) return;
		if (_navigationAgent.IsNavigationFinished()) return;

		// 1) Next waypoint & flat dir
		Vector3 next = _navigationAgent.GetNextPathPosition();
		Vector3 to = next - GlobalPosition;
		to.Y = 0f;
		if (to.LengthSquared() < 0.0001f) return;

		Vector3 desiredDir = to.Normalized();

		// 2) Current & target yaw
		float currentYaw = Rotation.Y; // node's yaw
		float targetYaw = Mathf.Atan2(desiredDir.X, desiredDir.Z);
		if (!_meshFacesPlusZ) targetYaw += Mathf.Pi; // if your model faces -Z

		float maxStep = Mathf.DegToRad(_rotationSpeed) * (float)delta;
		float windowIn = Mathf.DegToRad(_facingWindowDeg);
		float windowOut = Mathf.DegToRad(Mathf.Max(_stopWindowDeg, _facingWindowDeg + 0.1f)); // ensure > windowIn

		// Shortest signed delta in (-PI, PI]
		float diff = Mathf.AngleDifference(currentYaw, targetYaw);

		// 3) Hysteresis state update
		if (_moving && Mathf.Abs(diff) > windowOut) _moving = false;
		else if (!_moving && Mathf.Abs(diff) <= windowIn) _moving = true;

		// 4) Act on state
		if (!_moving)
		{
			// Rotate-in-place using short-arc step
			float step = Mathf.Clamp(diff, -maxStep, maxStep);
			float newYaw = currentYaw + step;
			Rotation = new Vector3(0f, newYaw, 0f);

			if (_navigationAgent.AvoidanceEnabled) _navigationAgent.Velocity = Vector3.Zero;
			else OnVelocityComputed(Vector3.Zero);
			return;
		}
		else
		{
			// Move forward
			Vector3 vel = desiredDir * Speed;
			if (_navigationAgent.AvoidanceEnabled) _navigationAgent.Velocity = vel;
			else OnVelocityComputed(vel);

			// Keep steering toward target while moving (short-arc)
			float step = Mathf.Clamp(diff, -maxStep, maxStep);
			float newYaw = currentYaw + step;
			Rotation = new Vector3(0f, newYaw, 0f);
		}
	}

	private void OnVelocityComputed(Vector3 safeVelocity)
	{
		Velocity = safeVelocity;
		MoveAndSlide();
	}

	private void ToggleSelectBorder() => _selectBorder.Visible = _selected;

	private void SetTeamColor(Node node, Color color)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D mi && mi.Mesh != null)
			{
				var mesh = mi.Mesh;
				int sc = mesh.GetSurfaceCount();
				if (sc == 0)
				{
					// No surfaces to edit; try material_override as a fallback
					if (mi.MaterialOverride is ShaderMaterial smo)
					{
						var dup = (ShaderMaterial)smo.Duplicate(true);
						dup.SetShaderParameter("team_color", color);
						mi.MaterialOverride = dup;
					}
				}
				else
				{
					for (int s = 0; s < sc; s++)
					{
						// Priority: per-surface override → mesh surface material → node material_override
						Material m =
							mi.GetSurfaceOverrideMaterial(s) ??
							mesh.SurfaceGetMaterial(s) ??
							mi.MaterialOverride;

						if (m is ShaderMaterial sm)
						{
							var dup = (ShaderMaterial)sm.Duplicate(true);
							dup.SetShaderParameter("team_color", color);
							// Put it back as a per-surface override so we don't mutate shared assets
							mi.SetSurfaceOverrideMaterial(s, dup);
						}
						else
						{
							// If you *know* everything should use your team shader, you can force it:
							// var forced = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/team_outline.shader") };
							// forced.SetShaderParameter("team_color", color);
							// mi.SetSurfaceOverrideMaterial(s, forced);
							// Otherwise just skip non-shader materials.
						}
					}
				}
			}

			// Recurse
			SetTeamColor(child, color);
		}
	}
}
