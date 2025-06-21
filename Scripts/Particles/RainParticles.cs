using Godot;

public partial class RainParticles : GpuParticles3D
{
	public override void _Ready()
	{
		Emitting = SceneResources.Instance.RainyWeather;
	}
}
