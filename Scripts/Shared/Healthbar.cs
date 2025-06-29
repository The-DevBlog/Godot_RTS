using Godot;

public partial class Healthbar : Sprite3D
{
	[Export] public float DesiredHeightPx = 32f;
	private Camera3D _cam;

	public override void _Ready()
	{
		_cam = GetViewport().GetCamera3D();
	}

	public override void _Process(double delta)
	{
		ScaleHealthbar();
	}

	private void ScaleHealthbar()
	{
		// 1) distance
		float d = GlobalPosition.DistanceTo(_cam.GlobalPosition);
		// 2) vertical FOV in radians
		float vfov = _cam.Fov * (Mathf.Pi / 180f);
		// 3) voxels per pixel
		float viewportH = GetViewport().GetVisibleRect().Size.Y;
		float worldPerPx = 2f * d * Mathf.Tan(vfov * 0.5f) / viewportH;
		// 4) apply only to Y
		Scale = new Vector3(
			Scale.X,
			DesiredHeightPx * worldPerPx,
			Scale.Z
		);
	}
}
