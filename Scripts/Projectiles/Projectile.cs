using Godot;

public partial class Projectile : Node3D
{
	[Export] private float _speed = 1250f;
	[Export] private float _lifetime = 3f;
	[Export] private int _team = 0; // so you can ignore friendlies via layers/masks
	private Node3D _impactParticles;
	private GpuParticles3D _trail;
	public int Damage;
	private Vector3 _vel;
	private float _timeLeft;
	private Node _shooter;           // optional reference for attribution
	private float _armTime = 0.00f;  // ignore collisions for a short time to avoid hitting self
	[Signal] public delegate void OnAttackEventHandler(Unit target, int dps);

	public override void _Ready()
	{
		_impactParticles = GetNode<Node3D>("ImpactParticles");
		Utils.NullCheck(_impactParticles);

		_trail = GetNode<GpuParticles3D>("Trail");
		Utils.NullCheck(_trail);
	}

	// private uint EnemyMaskForTeam(int team)
	// {
	//     // Example for 2 teams; expand as needed.
	//     return team == 0
	//         ? LAYER_WORLD | LAYER_TEAM1
	//         : LAYER_WORLD | LAYER_TEAM0;
	// }


	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if ((_timeLeft -= dt) <= 0f) { QueueFree(); return; }

		Vector3 from = GlobalPosition;
		Vector3 to = from + _vel * dt;

		var space = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithBodies = true;
		query.CollideWithAreas = true;

		// We'll add friendlies to this as we find them
		var exclude = new Godot.Collections.Array<Rid>();

		while (_armTime <= 0f) // only process hits after arming
		{
			query.Exclude = exclude;
			var hit = space.IntersectRay(query);
			if (hit.Count == 0)
				break; // nothing left along the segment => pass through

			var collider = hit["collider"].AsGodotObject() as Node;

			// If friendly, skip it and recast
			if (collider is Unit u && u.Team == _team)
			{
				exclude.Add((Rid)hit["rid"]);   // skip this body next time
				continue;                       // try again from same from->to
			}

			// Non-friendly (enemy or world) -> resolve hit
			var pos = (Vector3)hit["position"];
			var nrm = ((Vector3)hit["normal"]).Normalized();

			if (collider is IDamageable dmg)
			{
				dmg.ApplyDamage(Damage, pos, nrm);
				PlayImpactParticles(pos, nrm);
			}

			DetachTrail();
			QueueFree();
			return;
		}

		// No eligible hit => keep flying
		_armTime -= dt;
		GlobalPosition = to;
	}

	public void FireFrom(Transform3D muzzleXform, Vector3 velocity, Node shooter, int team)
	{
		GlobalTransform = muzzleXform;
		_vel = velocity * _speed;
		_timeLeft = _lifetime;
		_shooter = shooter;
		_team = team;
	}

	private void PlayImpactParticles(Vector3 pos, Vector3 normal)
	{
		// Detach so projectile despawn won’t kill the FX
		_impactParticles.GetParent().RemoveChild(_impactParticles);
		GetTree().CurrentScene.AddChild(_impactParticles);

		var up = Mathf.Abs(normal.Y) > 0.9f ? Vector3.Forward : Vector3.Up;
		var xf = Transform3D.Identity
			.Translated(pos + normal * 0.05f)
			.LookingAt(pos + normal * 1.05f, up);
		_impactParticles.GlobalTransform = xf;

		foreach (GpuParticles3D particles in _impactParticles.GetChildren())
			particles.Restart();

		// IMPORTANT: don’t bind through 'this' (projectile). Bind directly to the FX node.
		var fx = _impactParticles; // capture local
		var stt = GetTree().CreateTimer(3f);
		stt.Timeout += fx.QueueFree;  // engine calls fx.queue_free()
	}

	// Detach trail from projectile so it can finish
	// Delete after one second
	private void DetachTrail()
	{
		if (_trail == null || !IsInstanceValid(_trail)) return;

		var root = GetTree().CurrentScene;

		// Keep world transform while reparenting
		_trail.Reparent(root, true); // keepGlobalTransform = true

		// Capture a local so we don't rely on the field after we null it
		var trail = _trail;
		_trail = null;

		// Timer lives under the root (NOT the projectile, not the trail)
		var timer = new Timer
		{
			OneShot = true,
			Autostart = true,
			WaitTime = 1.0
		};

		root.AddChild(timer);

		timer.Timeout += () =>
		{
			if (IsInstanceValid(trail))
				trail.QueueFree();

			timer.QueueFree();
		};
	}

}
