using Godot;

public partial class Tracer : Node3D
{
	[Export] public Vector3 TargetPos { get; set; } = Vector3.Zero;
	[Export] public float TracerLength { get; set; } = 1f;
	public float Speed { get; set; }
	private Node3D _impactParticles;

	private const ulong MAX_LIFETIME_MS = 5000;
	private ulong _spawnTime;

	public override void _Ready()
	{
		_impactParticles = GetNode<Node3D>("ImpactParticles");
		_spawnTime = Time.GetTicksMsec();

		Utils.NullCheck(_impactParticles);
	}

	public override void _Process(double delta)
	{
		var diff = TargetPos - GlobalPosition;
		var step = diff.Normalized() * Speed * (float)delta;

		// Clamp step so we don't overshoot the target
		if (step.Length() > diff.Length())
			step = diff; // move exactly to target this frame

		GlobalPosition += step;

		// Despawn when close enough or too old
		var distLeft = (TargetPos - GlobalPosition).Length();
		var aliveMs = Time.GetTicksMsec() - _spawnTime;

		if (distLeft <= TracerLength || aliveMs > MAX_LIFETIME_MS)
			QueueFree();
	}

	public void PlayImpactParticles(Vector3 pos, Vector3 normal)
	{
		// Detach so projectile despawn won’t kill the FX
		_impactParticles.GetParent().RemoveChild(_impactParticles);
		GetTree().CurrentScene.AddChild(_impactParticles);

		// Ensure a valid forward (fallbacks if normal is zero)
		Vector3 fwd = normal;
		if (fwd.LengthSquared() < 1e-8f)
			fwd = (-GlobalTransform.Basis.Z);  // object's forward as fallback
		if (fwd.LengthSquared() < 1e-8f)
			fwd = Vector3.Up;                  // absolute fallback
		fwd = fwd.Normalized();

		// Pick a stable up that's not parallel to fwd
		Vector3 up = Mathf.Abs(fwd.Dot(Vector3.Up)) > 0.98f ? Vector3.Right : Vector3.Up;

		// Build transform with a guaranteed non-zero target delta
		var origin = pos + fwd * 0.05f;
		var target = origin + fwd * 1.0f;

		var xf = Transform3D.Identity.Translated(origin).LookingAt(target, up);
		_impactParticles.GlobalTransform = xf;

		foreach (GpuParticles3D particles in _impactParticles.GetChildren())
			particles.Restart();

		var fx = _impactParticles;
		var stt = GetTree().CreateTimer(3f);
		stt.Timeout += fx.QueueFree;
	}

}
