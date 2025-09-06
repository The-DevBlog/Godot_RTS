using System.Collections.Generic;
using Godot;

public partial class CombatSystem : Node
{
	[Export] private Unit _unit;
	[Export] private float AcquireHz = 5f;          // how often to re-acquire a target (times/sec)
	[Export] private float TurnSpeedDeg = 220f;     // tank yaw speed (deg/sec)
	[Export] private string TurretPath = "Model/Rig/Turret";
	[Export] private string MuzzlePath = "Model/Rig/Turret/Muzzle";
	private AnimationPlayer _animationPlayer;
	private Node3D _turret;
	private readonly List<Node3D> _muzzles = new();
	private RandomNumberGenerator _random;
	private bool _isZeroed;
	private int _hp;
	private int _dps;
	private int _range;
	private Unit _currentTarget;
	private float _fireRateTimer;
	private AudioStreamPlayer3D _attackSound;
	private Node3D _muzzleFlashParticles;
	private MyModels _models;
	private float _acquireTimer = 0f;

	// barrel selection
	private int _barrelIdx = 0;

	// sockets change (deferred rebuild) bookkeeping
	private Node3D _pendingYaw, _pendingMuzzle;
	private bool _socketsDirty;

	public override void _Ready()
	{
		Utils.NullExportCheck(_unit);

		_hp = _unit.CurrentHP;
		_dps = _unit.DPS;
		_range = _unit.Range;

		_models = AssetServer.Instance.Models;

		_random = new RandomNumberGenerator();
		_random.Randomize();

		_turret = _unit.GetNode<Node3D>(TurretPath);
		_animationPlayer = _unit.GetNodeOrNull<AnimationPlayer>("Model/AnimationPlayer");

		// initial muzzle collection: scan the Muzzle container's children
		_muzzles.Clear();
		var muzzleContainer = _unit.GetNodeOrNull<Node3D>(MuzzlePath); // SAFE: OrNull
		CollectMuzzlesFrom(muzzleContainer);
		if (_muzzles.Count == 0)
		{
			// Fallbacks (in case the scene is single-muzzle or slightly different)
			CollectMuzzlesFrom(_turret?.GetNodeOrNull<Node3D>("Muzzle")); // single-node-as-muzzle
			if (_muzzles.Count == 0) CollectMuzzlesFrom(_turret);        // rare: muzzles directly under turret
		}

		_attackSound = _unit.GetNode<AudioStreamPlayer3D>("Audio/Attack");
		_muzzleFlashParticles = _unit.GetNode<Node3D>("MuzzleFlash");

		_unit.LODManager.SocketsChanged += OnSocketsChanged;

		Utils.NullCheck(_turret);
		Utils.NullCheck(_attackSound);
		Utils.NullCheck(_muzzleFlashParticles);
		Utils.NullCheck(_animationPlayer);

		// If LOD sockets are already available, rebuild now (deferred)
		if (_unit.LODManager.TurretYaw != null)
			OnSocketsChanged(_unit.LODManager.TurretYaw, _unit.LODManager.Muzzle, _animationPlayer);

	}

	public override void _PhysicsProcess(double delta)
	{
		if (_turret == null || _muzzles.Count == 0)
			return;

		_acquireTimer -= (float)delta;
		if (_acquireTimer <= 0f)
		{
			_currentTarget = GetNearestEnemyInRange();   // single place to reacquire
			_acquireTimer = 1f / Mathf.Max(0.01f, AcquireHz);
		}

		FaceTarget((float)delta);
		TryAttack(delta);
	}

	// --- Sockets / Muzzles management ------------------------------------------------------------

	private void OnSocketsChanged(Node3D yaw, Node3D muzzle, AnimationPlayer animationPlayer)
	{
		// Defer rebuild to avoid racing with frees/swaps
		_pendingYaw = yaw;
		_pendingMuzzle = muzzle; // this should be the Muzzle CONTAINER: Rig/Turret/Muzzle
		_animationPlayer = animationPlayer;

		if (!_socketsDirty)
		{
			_socketsDirty = true;
			CallDeferred(nameof(RebuildSocketsDeferred));
		}
	}

	private void RebuildSocketsDeferred()
	{
		_socketsDirty = false;

		_turret = _pendingYaw;
		_muzzles.Clear();

		// Prefer scanning the Muzzle container's children (Muzzle1, Muzzle2, ...)
		Node3D scanRoot = _pendingMuzzle
			?? _turret?.GetNodeOrNull<Node3D>("Muzzle")   // fallback if LOD doesn't pass the container
			?? _turret;                                   // last resort

		CollectMuzzlesFrom(scanRoot);

		// Drop dead / out-of-tree refs
		_muzzles.RemoveAll(m => m == null || !GodotObject.IsInstanceValid(m) || !m.IsInsideTree());

		if (_muzzles.Count == 0)
			GD.PushError("CombatSystem: No muzzles after sockets change.");

		// Reset barrel index safely
		_barrelIdx = Mathf.PosMod(_barrelIdx, Mathf.Max(1, _muzzles.Count));
	}

	private void CollectMuzzlesFrom(Node3D root)
	{
		if (root == null) return;

		// If this root has children named Muzzle*, collect those (typical case: container with Muzzle1..N)
		bool addedChild = false;
		foreach (var child in root.GetChildren())
		{
			if (child is Node3D n3 && n3.Name.ToString().StartsWith("Muzzle"))
			{
				_muzzles.Add(n3);
				addedChild = true;
			}
		}

		// If no child muzzles were found and the root itself is a "Muzzle", treat it as a single muzzle
		if (!addedChild && root.Name.ToString().StartsWith("Muzzle"))
			_muzzles.Add(root);
	}

	private bool EnsureMuzzlesFresh()
	{
		bool removed = false;
		for (int i = _muzzles.Count - 1; i >= 0; --i)
		{
			if (_muzzles[i] == null || !GodotObject.IsInstanceValid(_muzzles[i]) || !_muzzles[i].IsInsideTree())
			{
				_muzzles.RemoveAt(i);
				removed = true;
			}
		}

		if (removed && _muzzles.Count == 0)
		{
			// attempt to rebuild from current sockets
			OnSocketsChanged(_unit.LODManager.TurretYaw, _unit.LODManager.Muzzle, _unit.LODManager.AnimationPlayer);
		}

		return _muzzles.Count > 0;
	}

	// --- Combat ----------------------------------------------------------------------------------

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

		if (!EnsureMuzzlesFresh()) return;

		// Choose barrel (Alternate pattern)
		if (_barrelIdx >= _muzzles.Count) _barrelIdx = 0;
		var muzzleNode = _muzzles[_barrelIdx++];
		if (muzzleNode == null || !GodotObject.IsInstanceValid(muzzleNode) || !muzzleNode.IsInsideTree())
			return;

		// choose fire mode
		var projectile = _models.Projectiles[_unit.WeaponType].Instantiate();
		if (_unit.WeaponType == MyEnums.WeaponType.SmallArms)
		{
			SpawnTracerFrom(muzzleNode, projectile as Tracer);
		}
		else
		{
			SpawnProjectileFrom(muzzleNode, projectile as Projectile);
		}

		// audio + vfx at the selected live muzzle
		_attackSound.Play();
		_muzzleFlashParticles.GlobalTransform = muzzleNode.GlobalTransform;
		foreach (GpuParticles3D p in _muzzleFlashParticles.GetChildren()) p.Restart();
		_animationPlayer?.Play($"Recoil{_barrelIdx}");
	}

	private void SpawnTracerFrom(Node3D muzzleNode, Tracer tracer)
	{
		GetTree().CurrentScene.AddChild(tracer);

		Transform3D muzzle = muzzleNode.GlobalTransform;

		// Aim center-mass
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
		var q = PhysicsRayQueryParameters3D.Create(from, to);
		q.CollideWithBodies = true;
		q.CollideWithAreas = true;

		// Skip friendlies: loop until enemy/world hit
		var exclude = new Godot.Collections.Array<Rid>();
		Vector3 endPos = to;
		while (true)
		{
			q.Exclude = exclude;
			var hit = space.IntersectRay(q);
			if (hit.Count == 0) break;

			var collider = hit["collider"].AsGodotObject() as Node;
			var pos = (Vector3)hit["position"];
			var nrm = ((Vector3)hit["normal"]).Normalized();

			if (collider is Unit u && u.Team == _unit.Team)
			{
				exclude.Add((Rid)hit["rid"]);   // skip friendly and recast
				continue;
			}

			endPos = pos;

			if (collider is IDamageable dmg)
			{
				dmg.ApplyDamage(_dps, pos, nrm);
			}

			tracer.PlayImpactParticles(pos, nrm);
			break;
		}

		// Spawn tracer purely as VFX
		tracer.GlobalPosition = from + dir * 0.25f; // tiny offset from muzzle
		tracer.TargetPos = endPos;
		tracer.Speed = _unit.ProjectileSpeed;
		tracer.TracerLength = 0.1f;
		tracer.LookAt(endPos);
	}

	private void SpawnProjectileFrom(Node3D muzzleNode, Projectile projectile)
	{
		projectile.Damage = _dps;
		GetTree().CurrentScene.AddChild(projectile);

		Transform3D muzzle = muzzleNode.GlobalTransform;

		// Aim center-mass (prefer a child "CollisionShape3D")
		Vector3 targetPos = _currentTarget.GlobalPosition;
		var aimCenter = _currentTarget.GetNodeOrNull<Node3D>("CollisionShape3D");
		if (aimCenter != null)
			targetPos = aimCenter.GlobalPosition;
		else
			Utils.PrintErr("No CollisionShape3D on target; using rough offset");

		Vector3 idealDir = (targetPos - muzzle.Origin).Normalized();
		Vector3 dir = AddBulletSpread(idealDir, _unit.BulletSpread, _random);

		projectile.FireFrom(muzzle, dir, _unit, _unit.Team);
	}

	// --- Targeting / Aiming ----------------------------------------------------------------------

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
			if (other.CurrentHP <= 0) continue;     // skip dead
			if (other.Team == myTeam) continue;     // skip friendlies

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
		float current = _turret.Rotation.Y;
		float err = WrapAngle(desiredLocalYaw - current);

		float maxStep = Mathf.DegToRad(speedDeg) * dt;
		float step = Mathf.Clamp(err, -maxStep, maxStep);
		_turret.Rotation = new Vector3(_turret.Rotation.X, current + step, _turret.Rotation.Z);

		// recompute error after stepping
		float newErr = WrapAngle(desiredLocalYaw - _turret.Rotation.Y);
		return Mathf.Abs(newErr) <= Mathf.DegToRad(3f); // tolerance
	}

	private void FaceTarget(float dt)
	{
		if (!IsInstanceValid(_currentTarget))
		{
			// No target: park turret to default
			_isZeroed = RotateTurretTowardsLocalYaw(Mathf.DegToRad(0f), TurnSpeedDeg, dt);
			return;
		}

		// normal aiming code...
		Vector3 tPos = _turret.GlobalPosition;
		Vector3 toTarget = _currentTarget.GlobalPosition - tPos;
		toTarget.Y = 0f;

		if (toTarget.LengthSquared() < 1e-6f) { _isZeroed = true; return; }

		float targetYawWorld = Mathf.Atan2(-toTarget.X, -toTarget.Z);
		float hullYawWorld = _unit.GlobalRotation.Y;
		float desiredLocalYaw = WrapAngle(targetYawWorld - hullYawWorld);

		_isZeroed = RotateTurretTowardsLocalYaw(desiredLocalYaw, TurnSpeedDeg, dt);
	}

	// --- Math helpers ----------------------------------------------------------------------------

	private static float WrapAngle(float a) => Mathf.PosMod(a + Mathf.Pi, Mathf.Tau) - Mathf.Pi;
	private static float ClampAngle(float a, float min, float max) => Mathf.Clamp(WrapAngle(a), min, max);

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

		// Normal offset
		Vector3 offset = (Mathf.Cos(phi) * right + Mathf.Sin(phi) * up) * sinTheta;

		// Scale vertical (up/down) contribution by 0.5
		offset = (offset.Dot(right) * right) + (offset.Dot(up) * 0.5f * up);

		return (forward * cosTheta + offset).Normalized();
	}
}
