
using Godot;

public partial class CombatSystem : Node
{
	[Export] private Unit _unit;
	[Export] private float AcquireHz = 5f;          // how often to re-acquire a target (times/sec)
	[Export] private float TurnSpeedDeg = 220f;     // tank yaw speed (deg/sec)
	[Export] private AnimationPlayer _animationPlayer;
	[Export] private Node3D _turretYaw;
	[Export] private Node3D _projectileSpawnPoint;
	[Export] private PackedScene _projectileScene;
	private bool _isZeroed;
	private int _hp;
	private int _dps;
	private int _range;
	private Unit _currentTarget;
	private float _fireRateTimer;
	private AudioStreamPlayer3D _attackSound;
	private Node3D _muzzleFlashParticles;

	public override void _Ready()
	{
		Utils.NullExportCheck(_unit);
		Utils.NullExportCheck(_turretYaw);
		Utils.NullExportCheck(_projectileScene);
		Utils.NullExportCheck(_projectileSpawnPoint);

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
		FaceTarget((float)delta);
		TryAttack(delta);
	}

	private void TryAttack(double delta)
	{
		if (!_isZeroed)
			return;

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

		// spawn projectile
		SpawnProjectile();

		// audio
		_attackSound.Play();

		// vfx
		foreach (GpuParticles3D particles in _muzzleFlashParticles.GetChildren())
			particles.Restart();

		// animation
		if (_animationPlayer != null)
			_animationPlayer.Play("FireAnimation");
	}

	private void SpawnProjectile()
	{
		Projectile projectile = _projectileScene.Instantiate<Projectile>();
		projectile.Damage = _dps;
		GetTree().CurrentScene.AddChild(projectile);

		// Shell goes along barrel forward (-Z in Godot)
		Transform3D muzzleXf = _projectileSpawnPoint.GlobalTransform;
		Vector3 dir = -muzzleXf.Basis.Z; // barrel forward
		projectile.FireFrom(muzzleXf, dir, _unit, _unit.Team); // set speed to taste
	}

	private Unit GetNearestEnemyInRange()
	{
		if (_unit == null) return null;

		Vector3 myPos = _unit.GlobalPosition;
		int myTeam = _unit.Team;
		float rangeSq = _range * _range;

		float bestDistSq = float.MaxValue;
		Unit best = null;

		foreach (Node n in GetTree().GetNodesInGroup(MyEnums.Group.units.ToString()))
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

	private bool RotateTurretTowardsLocalYaw(float desiredLocalYaw, float speedDeg, float dt)
	{
		float current = _turretYaw.Rotation.Y;
		float err = WrapAngle(desiredLocalYaw - current);

		float maxStep = Mathf.DegToRad(speedDeg) * dt;
		float step = Mathf.Clamp(err, -maxStep, maxStep);
		_turretYaw.Rotation = new Vector3(_turretYaw.Rotation.X, current + step, _turretYaw.Rotation.Z);

		// recompute error after stepping
		float newErr = WrapAngle(desiredLocalYaw - _turretYaw.Rotation.Y);
		return Mathf.Abs(newErr) <= Mathf.DegToRad(3f); // change tolerance as needed
	}

	private void FaceTarget(float dt)
	{
		_currentTarget = GetNearestEnemyInRange();

		if (!IsInstanceValid(_currentTarget))
		{
			// No target: park the turret facing the hull's front (local yaw = HomeLocalYawDeg)
			_isZeroed = RotateTurretTowardsLocalYaw(Mathf.DegToRad(0f), TurnSpeedDeg, dt);
			return;
		}

		// --- compute desired local yaw toward target ---
		Vector3 tPos = _turretYaw.GlobalPosition;
		Vector3 toTarget = _currentTarget.GlobalPosition - tPos;
		toTarget.Y = 0f;
		if (toTarget.LengthSquared() < 1e-6f) { _isZeroed = true; return; }

		float targetYawWorld = Mathf.Atan2(-toTarget.X, -toTarget.Z);
		float hullYawWorld = _unit.GlobalRotation.Y;
		float desiredLocalYaw = WrapAngle(targetYawWorld - hullYawWorld);

		// Slew toward the target and set zeroed flag
		_isZeroed = RotateTurretTowardsLocalYaw(desiredLocalYaw, TurnSpeedDeg, dt);
	}

	private static float WrapAngle(float a) => Mathf.PosMod(a + Mathf.Pi, Mathf.Tau) - Mathf.Pi;
	private static float ClampAngle(float a, float min, float max) => Mathf.Clamp(WrapAngle(a), min, max);
}
