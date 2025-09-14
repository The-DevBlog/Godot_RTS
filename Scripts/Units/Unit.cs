using Godot;
using MyEnums;

public partial class Unit : CharacterBody3D, ICostProvider, IDamageable
{
	[Export] public int Team { get; set; }
	[Export] public int Speed { get; set; }
	[Export] public int HP { get; set; }

	[Export] public int Cost { get; set; }
	[Export] public int BuildTime { get; set; }
	[Export] public int Acceleration { get; set; }
	[Export] public bool DebugEnabled { get; set; }
	[Export] public Node3D Death;
	[Export] public float MiniMapRadius { get; set; }
	[Export] private float _rotationSpeed = 220f;

	[ExportCategory("Weapons")]
	[Export] public WeaponSystem PrimaryWeaponSystem { get; set; }

	[ExportCategory("Unit Systems")]
	[Export] public LODManager LODManager;
	[Export] private HealthSystem _healthSystem;

	public AnimationPlayer AnimationPlayer;
	private Node3D _model;
	private float _facingWindowDeg = 10f; // start moving when |diff| <= this
	private float _stopWindowDeg = 18f;   // stop moving when |diff| > this (hysteresis)
	private bool _meshFacesPlusZ = false; // set false if your mesh faces -Z, eg: travel backwards or forwards
	private protected bool _moving = false; // hysteresis state
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
		_targetPosition = Vector3.Zero;
		_cam = GetViewport().GetCamera3D();

		AnimationPlayer = GetNode<AnimationPlayer>("Model/AnimationPlayer");

		if (HP == 0) Utils.PrintErr("No HP Assigned to unit");
		if (Speed == 0) Utils.PrintErr("No Speed Assigned to unit");
		if (Cost == 0) Utils.PrintErr("No Cost Assigned to unit");
		if (BuildTime == 0) Utils.PrintErr("No BuildTime Assigned to unit");
		if (Acceleration == 0) Utils.PrintErr("No Acceleration Assigned to unit");
		if (Team == 0) Utils.PrintErr("No Team Assigned to unit");
		if (MiniMapRadius == 0) Utils.PrintErr("No MiniMapRadius Assigned to unit");

		Utils.NullCheck(AnimationPlayer);
		Utils.NullExportCheck(PrimaryWeaponSystem);
		Utils.NullExportCheck(_healthSystem);
		Utils.NullExportCheck(LODManager);
		Utils.NullExportCheck(Death);

		CurrentHP = HP;

		HeadLights();

		// SetTeamColor(_model, PlayerManager.Instance.HumanPlayer.Color);
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

	private protected virtual void MoveAnimation() { }

	private protected virtual void IdleAnimation() { }

	private void MoveUnit(double delta)
	{
		if (NavigationServer3D.MapGetIterationId(_navigationAgent.GetNavigationMap()) == 0) return;

		// If the path is done, ensure we flip _moving OFF exactly once
		if (_navigationAgent.IsNavigationFinished())
		{
			if (_moving) { _moving = false; IdleAnimation(); } // Stop/idle here
			if (_navigationAgent.AvoidanceEnabled) _navigationAgent.Velocity = Vector3.Zero;
			else OnVelocityComputed(Vector3.Zero);
			return;
		}

		Vector3 next = _navigationAgent.GetNextPathPosition();
		Vector3 to = next - GlobalPosition; to.Y = 0f;
		if (to.LengthSquared() < 0.0001f)
		{
			if (_moving) { _moving = false; IdleAnimation(); }
			return;
		}

		Vector3 desiredDir = to.Normalized();
		float currentYaw = Rotation.Y;
		float targetYaw = Mathf.Atan2(desiredDir.X, desiredDir.Z);
		if (!_meshFacesPlusZ) targetYaw += Mathf.Pi;

		float maxStep = Mathf.DegToRad(_rotationSpeed) * (float)delta;
		float windowIn = Mathf.DegToRad(_facingWindowDeg);
		float windowOut = Mathf.DegToRad(Mathf.Max(_stopWindowDeg, _facingWindowDeg + 0.1f));
		float diff = Mathf.AngleDifference(currentYaw, targetYaw);

		// ---- Edge detection (play once on transition) ----
		bool prev = _moving;
		bool now = _moving;

		if (now && Mathf.Abs(diff) > windowOut) now = false;
		else if (!now && Mathf.Abs(diff) <= windowIn) now = true;

		if (now != prev)
		{
			_moving = now;
			if (now) MoveAnimation();   // fired once on start
			else IdleAnimation();   // fired once on stop (optional)
		}
		else
		{
			_moving = now;
		}
		// --------------------------------------------------

		if (!_moving)
		{
			float step = Mathf.Clamp(diff, -maxStep, maxStep);
			Rotation = new Vector3(0f, currentYaw + step, 0f);
			if (_navigationAgent.AvoidanceEnabled) _navigationAgent.Velocity = Vector3.Zero;
			else OnVelocityComputed(Vector3.Zero);
			return;
		}
		else
		{
			Vector3 vel = desiredDir * Speed;
			if (_navigationAgent.AvoidanceEnabled) _navigationAgent.Velocity = vel;
			else OnVelocityComputed(vel);

			float step = Mathf.Clamp(diff, -maxStep, maxStep);
			Rotation = new Vector3(0f, currentYaw + step, 0f);
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

	private void HeadLights()
	{
		Light3D headlights = GetNodeOrNull<Light3D>("HeadLight");

		if (headlights != null && GlobalResources.Instance.TimeOfDay == TimeOfDay.Night)
			headlights.Visible = true;
		else
			headlights.Visible = false;
	}
}
