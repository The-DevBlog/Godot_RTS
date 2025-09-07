using System.Collections.Generic;
using Godot;
using MyEnums;

public partial class CombatSystem : Node
{
	public enum WeaponSlot { Primary, Secondary }

	[ExportCategory("Unit & Weapon")]
	[Export] private Unit _unit;
	[Export] private WeaponSystem _weaponSystem;
	[Export] private WeaponSlot _slot = WeaponSlot.Primary;

	[ExportCategory("Behaviour")]
	[Export] private float _acquireHz = 5f;
	[Export] private float _turnSpeedDeg = 220f;

	[ExportCategory("VFX/SFX (per-weapon)")]
	[Export] private Node3D _muzzleFlashParticles;
	[Export] private AudioStreamPlayer3D _attackSound;

	// (Legacy bootstrap removed to avoid races)

	private AnimationPlayer _animationPlayer;
	private Node3D _turret;
	private readonly List<Node3D> _muzzles = new(); // firing cycle
	private readonly List<Node3D> _pendingMuzzles = new();

	private RandomNumberGenerator _random;
	private bool _isZeroed;
	private int _hp, _dps, _range;
	private Unit _currentTarget;
	private float _fireRateTimer;
	private MyModels _models;
	private float _acquireTimer = 0f;
	private int _barrelIdx = 0;
	private bool _socketsDirty;

	private static bool Alive(Node n) => n != null && GodotObject.IsInstanceValid(n) && n.IsInsideTree();

	public override void _Ready()
	{
		Utils.NullExportCheck(_unit);
		Utils.NullExportCheck(_weaponSystem);
		Utils.NullExportCheck(_muzzleFlashParticles);
		Utils.NullExportCheck(_attackSound);

		_hp = _unit.CurrentHP;
		_dps = _weaponSystem.Dmg;
		_range = _weaponSystem.Range;
		_models = AssetServer.Instance.Models;

		_random = new RandomNumberGenerator(); _random.Randomize();

		// Subscribe FIRST; do not pull immediately (avoid races)
		if (_slot == WeaponSlot.Primary)
			_unit.LODManager.SocketsChangedPrimary += OnSocketsChangedList;
		else
			_unit.LODManager.SocketsChangedSecondary += OnSocketsChangedList;

		// Block processing until we get sockets
		_socketsDirty = true;
	}

	public override void _ExitTree()
	{
		if (_unit?.LODManager == null) return;
		if (_slot == WeaponSlot.Primary)
			_unit.LODManager.SocketsChangedPrimary -= OnSocketsChangedList;
		else
			_unit.LODManager.SocketsChangedSecondary -= OnSocketsChangedList;
	}

	private void DebugDumpRecoilTracks()
	{
		if (_animationPlayer == null) return;
		foreach (var nameObj in _animationPlayer.GetAnimationList())
		{
			var name = nameObj.ToString();
			if (!name.StartsWith("Recoil")) continue;

			var anim = _animationPlayer.GetAnimation(name);
			GD.Print($"[AnimDump] {name} tracks={anim.GetTrackCount()}");
			for (int i = 0; i < anim.GetTrackCount(); i++)
			{
				var path = anim.TrackGetPath(i);   // NodePath
				GD.Print($"  - track[{i}] -> {path}");
			}
		}
		foreach (var m in _muzzles)
			GD.Print($"[Muzzle] {m.Name} path={m.GetPath()}");
	}


	public override void _PhysicsProcess(double delta)
	{
		if (_socketsDirty) return;                 // wait for rebuild
		if (!Alive(_turret) || _muzzles.Count == 0) return;

		_acquireTimer -= (float)delta;
		if (_acquireTimer <= 0f)
		{
			_currentTarget = GetNearestEnemyInRange();
			_acquireTimer = 1f / Mathf.Max(0.01f, _acquireHz);
		}

		FaceTarget((float)delta);
		DebugDumpRecoilTracks();
		TryAttack(delta);
	}

	// ----- sockets from LODManager -----

	private void OnSocketsChangedList(Node3D yaw, IReadOnlyList<Node3D> muzzles, AnimationPlayer ap)
	{
		_animationPlayer = ap;
		_turret = yaw;

		_pendingMuzzles.Clear();
		if (muzzles != null)
			foreach (var m in muzzles)
				if (Alive(m)) _pendingMuzzles.Add(m);

		if (!_socketsDirty)
		{
			_socketsDirty = true;
			CallDeferred(nameof(RebuildSocketsDeferred));
		}
		else
		{
			// first time we’re still dirty; just schedule once
			CallDeferred(nameof(RebuildSocketsDeferred));
		}
	}

	private void RebuildSocketsDeferred()
	{
		_muzzles.Clear();
		foreach (var m in _pendingMuzzles)
			if (Alive(m)) _muzzles.Add(m);

		_muzzles.RemoveAll(m => m == null || !GodotObject.IsInstanceValid(m) || !m.IsInsideTree());

		// reset barrel index safely
		_barrelIdx = Mathf.PosMod(_barrelIdx, Mathf.Max(1, _muzzles.Count));
		_socketsDirty = false;
	}

	// ----- combat -----

	private void TryAttack(double delta)
	{
		if (!_isZeroed) return;
		if (!IsInstanceValid(_currentTarget)) { _currentTarget = null; return; }

		Vector3 distance = _currentTarget.GlobalPosition - _unit.GlobalPosition;
		distance.Y = 0;
		if (distance.LengthSquared() > (_range * _range)) return;

		_fireRateTimer = Mathf.Max(0f, _fireRateTimer - (float)delta);
		if (_fireRateTimer > 0f) return;
		_fireRateTimer = _weaponSystem.FireRate;

		if (!EnsureMuzzlesFresh()) return;

		if (_barrelIdx >= _muzzles.Count) _barrelIdx = 0;
		var muzzleNode = _muzzles[_barrelIdx++];
		if (!Alive(muzzleNode)) return;

		var projectile = _models.Projectiles[_weaponSystem.WeaponType].Instantiate();
		if (_weaponSystem.WeaponType == WeaponType.SmallArms)
			SpawnTracerFrom(muzzleNode, projectile as Tracer);
		else
			SpawnProjectileFrom(muzzleNode, projectile as Projectile);

		_attackSound.GlobalTransform = muzzleNode.GlobalTransform;
		_attackSound.Play();

		_muzzleFlashParticles.GlobalTransform = muzzleNode.GlobalTransform;
		foreach (GpuParticles3D p in _muzzleFlashParticles.GetChildren()) p.Restart();

		GD.Print("Firing from muzzle: " + muzzleNode.Name + ". Muzzle count: " + _muzzles.Count + ", idx: " + _barrelIdx + ", animation: " + "Recoil" + _barrelIdx);

		_animationPlayer?.Play($"Recoil{_barrelIdx}"); // matches the barrel that just fired
	}

	private void SpawnTracerFrom(Node3D muzzleNode, Tracer tracer)
	{
		GetTree().CurrentScene.AddChild(tracer);
		Transform3D muzzle = muzzleNode.GlobalTransform;

		Vector3 targetPos = _currentTarget.GlobalPosition;
		var aimCenter = _currentTarget.GetNodeOrNull<Node3D>("CollisionShape3D");
		if (aimCenter != null) targetPos = aimCenter.GlobalPosition; else targetPos += Vector3.Up * 1.0f;

		Vector3 idealDir = (targetPos - muzzle.Origin).Normalized();
		Vector3 dir = AddBulletSpread(idealDir, _weaponSystem.BulletSpread, _random);

		float maxDist = _weaponSystem.Range;
		Vector3 from = muzzle.Origin;
		Vector3 to = from + dir * maxDist;

		var space = GetViewport().GetWorld3D().DirectSpaceState;
		var q = PhysicsRayQueryParameters3D.Create(from, to);
		q.CollideWithBodies = true; q.CollideWithAreas = true;

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

			if (collider is Unit u && u.Team == _unit.Team) { exclude.Add((Rid)hit["rid"]); continue; }

			endPos = pos;
			if (collider is IDamageable dmg) dmg.ApplyDamage(_dps, pos, nrm);
			tracer.PlayImpactParticles(pos, nrm);
			break;
		}

		tracer.GlobalPosition = from + dir * 0.25f;
		tracer.TargetPos = endPos;
		tracer.Speed = _weaponSystem.ProjectileSpeed;
		tracer.TracerLength = 0.1f;
		tracer.LookAt(endPos);
	}

	private void SpawnProjectileFrom(Node3D muzzleNode, Projectile projectile)
	{
		projectile.Damage = _dps;
		GetTree().CurrentScene.AddChild(projectile);

		Transform3D muzzle = muzzleNode.GlobalTransform;

		Vector3 targetPos = _currentTarget.GlobalPosition;
		var aimCenter = _currentTarget.GetNodeOrNull<Node3D>("CollisionShape3D");
		if (aimCenter != null) targetPos = aimCenter.GlobalPosition;
		else Utils.PrintErr("No CollisionShape3D on target; using rough offset");

		Vector3 idealDir = (targetPos - muzzle.Origin).Normalized();
		Vector3 dir = AddBulletSpread(idealDir, _weaponSystem.BulletSpread, _random);

		projectile.FireFrom(muzzle, dir, _unit, _weaponSystem.ProjectileSpeed, _unit.Team);
	}

	// ----- targeting / aiming -----

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
			if (other.CurrentHP <= 0) continue;
			if (other.Team == myTeam) continue;

			Vector3 d = other.GlobalPosition - myPos; d.Y = 0;
			float dsq = d.LengthSquared();
			if (dsq <= rangeSq && dsq < bestDistSq) { bestDistSq = dsq; best = other; }
		}
		return best;
	}

	private bool RotateTurretTowardsLocalYaw(float desiredLocalYaw, float speedDeg, float dt)
	{
		if (!Alive(_turret)) return false;
		float current = _turret.Rotation.Y;
		float err = WrapAngle(desiredLocalYaw - current);
		float maxStep = Mathf.DegToRad(speedDeg) * dt;
		float step = Mathf.Clamp(err, -maxStep, maxStep);
		_turret.Rotation = new Vector3(_turret.Rotation.X, current + step, _turret.Rotation.Z);
		float newErr = WrapAngle(desiredLocalYaw - _turret.Rotation.Y);
		return Mathf.Abs(newErr) <= Mathf.DegToRad(3f);
	}

	private void FaceTarget(float dt)
	{
		if (!Alive(_turret)) { _isZeroed = true; return; }

		if (!IsInstanceValid(_currentTarget))
		{
			_isZeroed = RotateTurretTowardsLocalYaw(Mathf.DegToRad(0f), _turnSpeedDeg, dt);
			return;
		}

		Vector3 tPos = _turret.GlobalPosition;
		Vector3 toTarget = _currentTarget.GlobalPosition - tPos; toTarget.Y = 0f;
		if (toTarget.LengthSquared() < 1e-6f) { _isZeroed = true; return; }

		float targetYawWorld = Mathf.Atan2(-toTarget.X, -toTarget.Z);
		float hullYawWorld = _unit.GlobalRotation.Y;
		float desiredLocalYaw = WrapAngle(targetYawWorld - hullYawWorld);
		_isZeroed = RotateTurretTowardsLocalYaw(desiredLocalYaw, _turnSpeedDeg, dt);
	}

	private static float WrapAngle(float a) => Mathf.PosMod(a + Mathf.Pi, Mathf.Tau) - Mathf.Pi;

	private bool EnsureMuzzlesFresh()
	{
		for (int i = _muzzles.Count - 1; i >= 0; --i)
		{
			if (_muzzles[i] == null || !GodotObject.IsInstanceValid(_muzzles[i]) || !_muzzles[i].IsInsideTree())
				_muzzles.RemoveAt(i);
		}
		return _muzzles.Count > 0;
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
		offset = (offset.Dot(right) * right) + (offset.Dot(up) * 0.5f * up);
		return (forward * cosTheta + offset).Normalized();
	}
}
