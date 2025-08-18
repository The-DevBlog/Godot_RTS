// Projectile.cs
using Godot;

public partial class Projectile : Node3D
{
	[Export] public float Speed = 400f;
	[Export] public float Lifetime = 3f;
	[Export(PropertyHint.Layers3DPhysics)] public uint HitMask = 1 << 0; // set to "Units" & "World" layers in inspector
	[Export] public int Team = 0; // so you can ignore friendlies via layers/masks
	public int Damage;
	private Vector3 _vel;
	private float _timeLeft;
	private Node _shooter;           // optional reference for attribution
	private float _armTime = 0.04f;  // ignore collisions for a short time to avoid hitting self
	[Signal] public delegate void OnAttackEventHandler(Unit target, int dps);

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if ((_timeLeft -= dt) <= 0f) { QueueFree(); return; }

		// integrate motion
		Vector3 from = GlobalPosition;
		Vector3 step = _vel * dt;
		Vector3 to = from + step;

		// continuous collision: ray from -> to
		var space = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollisionMask = HitMask;

		var hit = space.IntersectRay(query);

		if (hit.Count > 0 && _armTime <= 0f)
		{
			var collider = hit["collider"].AsGodotObject() as Node;
			// Apply damage if it's a Unit (you can adjust to your own API)
			if (collider != null)
			{
				if (collider is IDamageable damageable)
				{
					GD.Print($"Projectile hit a damageable: {collider.Name}");
					damageable.ApplyDamage(Damage, (Vector3)hit["position"], (Vector3)hit["normal"]);
				}

				QueueFree();
				return;
			}

			// World hit or non-unit: place impact and destroy
			GlobalPosition = (Vector3)hit["position"];
			QueueFree();
			return;
		}

		_armTime -= dt;
		GlobalPosition = to; // no hit: continue
	}

	public void FireFrom(Transform3D muzzleXform, Vector3 velocity, Node shooter, int team)
	{
		GlobalTransform = muzzleXform;
		_vel = velocity * Speed;
		_timeLeft = Lifetime;
		_shooter = shooter;
		Team = team;
	}

}
