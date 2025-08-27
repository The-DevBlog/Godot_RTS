using Godot;

public partial class CombatSystem : Node
{
	[Export] private Unit _unit;
	[Export] private float AcquireHz = 5f;          // how often to re-acquire a target (times/sec) TODO: Add spatial partitioning to optimize target acquisition
	[Export] private float TurnSpeedDeg = 220f;     // tank yaw speed (deg/sec)
	[Export] private AnimationPlayer _animationPlayer;
	[Export] private Node3D _turretYaw;
	[Export] private Node3D _projectileSpawnPoint;
	[Export] private PackedScene _projectileScene;
	private RandomNumberGenerator _random;
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
		_random = new RandomNumberGenerator();
		_random.Randomize();

		_attackSound = GetNode<AudioStreamPlayer3D>("../../Audio/Attack");
		Utils.NullCheck(_attackSound);

		_muzzleFlashParticles = GetNode<Node3D>("../../MuzzleFlash");
		Utils.NullCheck(_muzzleFlashParticles);
	}

	public override void _PhysicsProcess(double delta)
	{
		_acquireTimer -= (float)delta;
		if (_acquireTimer <= 0f)
		{
			_currentTarget = GetNearestEnemyInRange();
			_acquireTimer = 1f / Mathf.Max(0.01f, AcquireHz);
		}


		FaceTarget((float)delta);
		TryAttack(delta);
	}

	private void TryAttack(double delta)
	{
		if (!_isZeroed) return;
		if (!IsInstanceValid(_currentTarget)) { _currentTarget = null; return; }

		Vector3 distance = _currentTarget.GlobalPosition - _unit.GlobalPosition;
		distance.Y = 0;
		if (distance.LengthSquared() > (_range * _range)) return;

		_fireRateTimer = Mathf.Max(0f, _fireRateTimer - (float)delta);
		if (_fireRateTimer > 0f) return;
		_fireRateTimer = _unit.FireRate;

		// --- choose fire mode ---
		if (_unit.WeaponType == MyEnums.WeaponType.SmallArms)   // <-- your enum/flag
			FireHitscan();
		else
			SpawnProjectile();

		// audio + vfx (muzzle)
		_attackSound.Play();
		foreach (GpuParticles3D p in _muzzleFlashParticles.GetChildren()) p.Restart();
		_animationPlayer?.Play("FireAnimation");
	}

	[Export] public float TracerChance = 0.33f;      // draw 1 of 3 bullets as a tracer
	[Export] public float TracerWidth = 0.035f;
	[Export] public float TracerLifetime = 0.1f;
	[Export] public PackedScene TracerScene;         // simple line/quad scene (below)

	private void FireHitscan()
	{
		Transform3D muzzle = _projectileSpawnPoint.GlobalTransform;

		// Aim at center-mass inline (uses CollisionShape3D center if present)
		Vector3 targetPos = _currentTarget.GlobalPosition;
		var aimCenter = _currentTarget.GetNodeOrNull<Node3D>("CollisionShape3D");
		if (aimCenter != null) targetPos = aimCenter.GlobalPosition;
		else targetPos += Vector3.Up * 1.0f;

		Vector3 idealDir = (targetPos - muzzle.Origin).Normalized();
		Vector3 dir = AddBulletSpread(idealDir, _unit.BulletSpread, _random);

		float maxDist = _unit.Range;
		Vector3 from = muzzle.Origin;
		Vector3 to = from + dir * maxDist;

		var space = GetViewport().GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithBodies = true;
		query.CollideWithAreas = true;
		// Optionally: query.CollisionMask = _unit.BulletMask;

		var hit = space.IntersectRay(query);

		Vector3 endPos = to;
		if (hit.Count > 0)
		{
			endPos = (Vector3)hit["position"];
			var nrm = ((Vector3)hit["normal"]).Normalized();

			if (hit["collider"].AsGodotObject() is IDamageable dmg)
				dmg.ApplyDamage(_dps, endPos, nrm);

			// (Optional) spawn an impact FX scene here
			// SpawnImpactFx(endPos, nrm);
		}

		// Tracer VFX sometimes (cheap)
		if (TracerScene != null && _random.Randf() < TracerChance)
		{
			var tracer = TracerScene.Instantiate<Node3D>() as ITracer;
			GetTree().CurrentScene.AddChild(tracer as Node);
			tracer!.Init(from, endPos, TracerWidth, TracerLifetime);
		}
	}


	// private void TryAttack(double delta)
	// {
	// 	if (!_isZeroed)
	// 		return;

	// 	if (!IsInstanceValid(_currentTarget))
	// 	{
	// 		_currentTarget = null;
	// 		return;
	// 	}

	// 	// Ensure target it still in range
	// 	Vector3 distance = _currentTarget.GlobalPosition - _unit.GlobalPosition;
	// 	distance.Y = 0;

	// 	if (distance.LengthSquared() > (_range * _range))
	// 		return;

	// 	// cooldown tick
	// 	_fireRateTimer = Mathf.Max(0f, _fireRateTimer - (float)delta);

	// 	if (_fireRateTimer > 0f)
	// 		return;

	// 	_fireRateTimer = _unit.FireRate;

	// 	// spawn projectile
	// 	SpawnProjectile();

	// 	// audio
	// 	_attackSound.Play();

	// 	// vfx
	// 	foreach (GpuParticles3D particles in _muzzleFlashParticles.GetChildren())
	// 		particles.Restart();

	// 	// animation
	// 	if (_animationPlayer != null)
	// 		_animationPlayer.Play("FireAnimation");
	// }

	private void SpawnProjectile()
	{
		var projectile = _projectileScene.Instantiate<Projectile>();
		projectile.Damage = _dps;
		GetTree().CurrentScene.AddChild(projectile);

		// Keep your existing transform (preserves trails/FX)
		Transform3D muzzle = _projectileSpawnPoint.GlobalTransform;

		// ---- Aim at center-mass (inline) ----
		// Prefer a child "AimCenter" on the target if it exists; otherwise use a simple Y offset.
		Vector3 targetPos = _currentTarget.GlobalPosition;
		var aimCenter = _currentTarget.GetNodeOrNull<Node3D>("CollisionShape3D");
		if (aimCenter != null)
			targetPos = aimCenter.GlobalPosition;
		else
		{
			GD.PrintErr("No AimCenter on target; using rough offset");
			targetPos += Vector3.Up * 1.0f; // <- tweak this height for your units
		}

		Vector3 idealDir = (targetPos - muzzle.Origin).Normalized();

		// ---- Add spread around the ideal 3D direction ----
		Vector3 dir = AddBulletSpread(idealDir, _unit.BulletSpread, _random); // uses your existing AddSpread()

		projectile.FireFrom(muzzle, dir, _unit, _unit.Team);
	}

	private static Vector3 AddBulletSpread(Vector3 forward, float maxDeg, RandomNumberGenerator rng)
	{
		forward = forward.Normalized();
		if (maxDeg <= 0.0001f) return forward;

		float maxRad = Mathf.DegToRad(maxDeg);
		float cosMax = Mathf.Cos(maxRad);
		float cosTheta = Mathf.Lerp(cosMax, 1f, rng.Randf());
		float sinTheta = Mathf.Sqrt(1f - cosTheta * cosTheta);
		float phi = rng.Randf() * Mathf.Tau;

		Vector3 any = Mathf.Abs(forward.Y) < 0.999f ? Vector3.Up : Vector3.Right;
		Vector3 right = forward.Cross(any).Normalized();
		Vector3 up = right.Cross(forward).Normalized();

		Vector3 offset = (Mathf.Cos(phi) * right + Mathf.Sin(phi) * up) * sinTheta;
		return (forward * cosTheta + offset).Normalized();
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

	private float _acquireTimer = 0f;

	private void FaceTarget(float dt)
	{
		_acquireTimer -= dt;
		if (_acquireTimer <= 0f)
		{
			_acquireTimer = 1f / AcquireHz;
			_currentTarget = GetNearestEnemyInRange();
		}

		if (!IsInstanceValid(_currentTarget))
		{
			// No target: park turret to default
			_isZeroed = RotateTurretTowardsLocalYaw(Mathf.DegToRad(0f), TurnSpeedDeg, dt);
			return;
		}

		// normal aiming code...
		Vector3 tPos = _turretYaw.GlobalPosition;
		Vector3 toTarget = _currentTarget.GlobalPosition - tPos;
		toTarget.Y = 0f;

		if (toTarget.LengthSquared() < 1e-6f) { _isZeroed = true; return; }

		float targetYawWorld = Mathf.Atan2(-toTarget.X, -toTarget.Z);
		float hullYawWorld = _unit.GlobalRotation.Y;
		float desiredLocalYaw = WrapAngle(targetYawWorld - hullYawWorld);

		_isZeroed = RotateTurretTowardsLocalYaw(desiredLocalYaw, TurnSpeedDeg, dt);
	}

	private static float WrapAngle(float a) => Mathf.PosMod(a + Mathf.Pi, Mathf.Tau) - Mathf.Pi;
	private static float ClampAngle(float a, float min, float max) => Mathf.Clamp(WrapAngle(a), min, max);
}
