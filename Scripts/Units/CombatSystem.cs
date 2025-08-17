
using Godot;

public partial class CombatSystem : Node
{
	[Export] private Unit _unit;
	[Export] private float AcquireHz = 5f;          // how often to re-acquire a target (times/sec)
	[Export] private float TurnSpeedDeg = 180f;     // tank yaw speed (deg/sec)
	[Export] private AnimationPlayer _animationPlayer;
	[Export] private Node3D _turretYaw;
	private int _hp;
	private int _dps;
	private int _range;
	private Unit _currentTarget;
	private float _fireRateTimer;
	private string UnitsGroup = MyEnums.Group.Units.ToString();
	private AudioStreamPlayer3D _attackSound;
	private Node3D _muzzleFlashParticles;
	[Signal] public delegate void OnAttackEventHandler(Unit target, int dps);

	public override void _Ready()
	{
		Utils.NullExportCheck(_unit);
		Utils.NullExportCheck(_animationPlayer);
		Utils.NullExportCheck(_turretYaw);

		_hp = _unit.CurrentHP;
		_dps = _unit.DPS;
		_range = _unit.Range;

		_attackSound = GetNode<AudioStreamPlayer3D>("../../Audio/Attack");
		Utils.NullCheck(_attackSound);

		_muzzleFlashParticles = GetNode<Node3D>("../../MuzzleFlash");
		Utils.NullCheck(_muzzleFlashParticles);
	}

	public override void _PhysicsProcess(double delta)
	{
		// TODO: Remove. For debugging only.
		// if (_unit.Team == 2)
		// 	return;

		FaceTarget((float)delta);
		TryAttack(delta);
	}

	private void TryAttack(double delta)
	{
		if (!IsInstanceValid(_currentTarget))
		{
			_currentTarget = null;
			return;
		}

		// Ensure target it still in range
		Vector3 distance = _currentTarget.GlobalPosition - _unit.GlobalPosition;
		distance.Y = 0;

		if (distance.LengthSquared() > (_range * _range))
			return;

		// cooldown tick
		_fireRateTimer = Mathf.Max(0f, _fireRateTimer - (float)delta);

		if (_fireRateTimer > 0f)
			return;

		_fireRateTimer = _unit.FireRate;

		EmitSignal(SignalName.OnAttack, _currentTarget, _dps);

		// audio
		_attackSound.Play();

		// vfx
		foreach (GpuParticles3D particles in _muzzleFlashParticles.GetChildren())
		{
			particles.Restart();
		}

		// animation
		_animationPlayer.Play("FireAnimation");
	}

	private Unit GetNearestEnemyInRange()
	{
		if (_unit == null) return null;

		Vector3 myPos = _unit.GlobalPosition;
		int myTeam = _unit.Team;
		float rangeSq = _range * _range;

		float bestDistSq = float.MaxValue;
		Unit best = null;

		foreach (Node n in GetTree().GetNodesInGroup(UnitsGroup))
		{
			if (n == _unit || n == null) continue;
			if (n is not Unit other) continue;
			if (other.CurrentHP <= 0) continue;           // skip dead
			if (other.Team == myTeam) continue;    // skip friendlies

			Vector3 d = other.GlobalPosition - myPos;
			d.Y = 0; // horizontal distance
			float dsq = d.LengthSquared();
			if (dsq <= rangeSq && dsq < bestDistSq)
			{
				bestDistSq = dsq;
				best = other;
			}
		}

		return best;
	}

	// private void FaceTowardsTarget(float delta)
	// {
	// 	_currentTarget = GetNearestEnemyInRange();
	// 	if (!IsInstanceValid(_currentTarget))
	// 		return;

	// 	Vector3 myPos = _unit.GlobalPosition;
	// 	Vector3 dir = _currentTarget.GlobalPosition - myPos;
	// 	dir.Y = 0;

	// 	if (dir.LengthSquared() < 1e-6f) return;

	// 	// Desired yaw (Godot: yaw around +Y; forward is -Z)
	// 	float targetYaw = Mathf.Atan2(-dir.X, -dir.Z);   // <- note the minus signs
	// 	float currentYaw = _unit.Rotation.Y;

	// 	// Step-limited turn toward the target yaw
	// 	float maxStep = Mathf.DegToRad(TurnSpeedDeg) * delta;
	// 	float deltaYaw = targetYaw - currentYaw;
	// 	while (deltaYaw > Mathf.Pi) deltaYaw -= Mathf.Tau;
	// 	while (deltaYaw < -Mathf.Pi) deltaYaw += Mathf.Tau;
	// 	float step = Mathf.Clamp(deltaYaw, -maxStep, maxStep);

	// 	_unit.Rotation = new Vector3(0f, currentYaw + step, 0f);
	// }
	private static float WrapAngle(float a) => Mathf.PosMod(a + Mathf.Pi, Mathf.Tau) - Mathf.Pi;
	private static float ClampAngle(float a, float min, float max) => Mathf.Clamp(WrapAngle(a), min, max);
	private void FaceTarget(float dt)
	{
		_currentTarget = GetNearestEnemyInRange();
		if (!IsInstanceValid(_currentTarget))
			return;

		// --- YAW (around +Y on the turret pivot) ---
		// Work in world space, then convert to local yaw relative to the hull.
		Vector3 tPos = _turretYaw.GlobalPosition;
		Vector3 toTarget = _currentTarget.GlobalPosition - tPos;
		toTarget.Y = 0f;
		if (toTarget.LengthSquared() < 1e-6f) return;

		// Godot forward is -Z; this matches your earlier Atan2.
		float targetYawWorld = Mathf.Atan2(-toTarget.X, -toTarget.Z);

		// Hull’s world yaw (assuming _unit is the hull/root)
		float hullYawWorld = _unit.GlobalRotation.Y;

		// Desired turret yaw *relative to the hull*
		float desiredLocalYaw = WrapAngle(targetYawWorld - hullYawWorld);

		float currentLocalYaw = _turretYaw.Rotation.Y;
		float maxStepYaw = Mathf.DegToRad(TurnSpeedDeg) * dt;
		float yawDelta = ClampAngle(desiredLocalYaw - currentLocalYaw, -maxStepYaw, maxStepYaw);
		_turretYaw.Rotation = new Vector3(_turretYaw.Rotation.X, currentLocalYaw + yawDelta, _turretYaw.Rotation.Z);

		// // --- PITCH (optional; rotate around local X on BarrelPitch) ---
		// if (_barrelPitch != null)
		// {
		//     // Aim using the barrel pivot’s local space so X is the correct pitch axis.
		//     Vector3 localDir = _barrelPitch.ToLocal(_currentTarget.GlobalPosition);
		//     // With -Z as forward, this gives positive pitch when aiming up.
		//     float desiredPitch = Mathf.Atan2(localDir.Y, -localDir.Z);

		//     float currentPitch = _barrelPitch.Rotation.X;
		//     float maxStepPitch = Mathf.DegToRad(PitchSpeedDeg) * dt;
		//     float clampedTarget = Mathf.Clamp(desiredPitch,
		//         Mathf.DegToRad(MinPitchDeg), Mathf.DegToRad(MaxPitchDeg));
		//     float pitchDelta = ClampAngle(clampedTarget - currentPitch, -maxStepPitch, maxStepPitch);
		//     _barrelPitch.Rotation = new Vector3(currentPitch + pitchDelta, _barrelPitch.Rotation.Y, _barrelPitch.Rotation.Z);
		// }
	}
}
