// Projectile.cs
using Godot;

public partial class Projectile : Node3D
{
	[Export] private float _speed = 1250f;
	[Export] private float _lifetime = 3f;
	[Export(PropertyHint.Layers3DPhysics)] private uint _hitMask = 1 << 0; // set to "Units" & "World" layers in inspector
	[Export] private int _team = 0; // so you can ignore friendlies via layers/masks
	private Node3D _impactParticles;
	public int Damage;
	private Vector3 _vel;
	private float _timeLeft;
	private Node _shooter;           // optional reference for attribution
	private float _armTime = 0.04f;  // ignore collisions for a short time to avoid hitting self
	[Signal] public delegate void OnAttackEventHandler(Unit target, int dps);

	public override void _Ready()
	{
		_impactParticles = GetNode<Node3D>("ImpactParticles");
		Utils.NullCheck(_impactParticles);
	}

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
		query.CollisionMask = _hitMask;

		var hit = space.IntersectRay(query);

		if (hit.Count > 0 && _armTime <= 0f)
		{
			var pos = (Vector3)hit["position"];
			var nrm = ((Vector3)hit["normal"]).Normalized();

			var collider = hit["collider"].AsGodotObject() as Node;
			// Apply damage if it's a Unit (you can adjust to your own API)
			if (collider != null)
			{
				if (collider is IDamageable damageable)
				{
					GD.Print($"Projectile hit a damageable: {collider.Name}");
					damageable.ApplyDamage(Damage, pos, nrm);
					PlayImpactParticles(pos, nrm);
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
		_vel = velocity * _speed;
		_timeLeft = _lifetime;
		_shooter = shooter;
		_team = team;
	}

	private void PlayImpactParticles(Vector3 pos, Vector3 normal, float lifetime = 0.5f)
	{
		// Detach if needed so QueueFree() on the projectile doesn’t kill the FX
		_impactParticles.GetParent().RemoveChild(_impactParticles);
		GetTree().CurrentScene.AddChild(_impactParticles);

		// Pick a stable up vector (avoid gimbal when normal ~ up)
		var up = Mathf.Abs(normal.Y) > 0.9f ? Vector3.Forward : Vector3.Up;

		// Place slightly off the surface and orient so -Z faces 'normal'
		var xf = Transform3D.Identity.Translated(pos + normal * 0.05f)
								   .LookingAt(pos + normal * 1.05f, up);
		_impactParticles.GlobalTransform = xf;

		foreach (GpuParticles3D particles in _impactParticles.GetChildren())
			particles.Restart();

		GetTree().CreateTimer(lifetime).Timeout += () => _impactParticles.QueueFree();
	}
}
