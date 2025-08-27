using Godot;

public partial class Tracer : Node3D
{
	[Export] public Vector3 TargetPos { get; set; } = Vector3.Zero;
	[Export] public float Speed { get; set; } = 75.0f;      // m/s
	[Export] public float TracerLength { get; set; } = 1f;

	private const ulong MAX_LIFETIME_MS = 5000;
	private ulong _spawnTime;

	public override void _Ready()
	{
		_spawnTime = Time.GetTicksMsec();
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
}
