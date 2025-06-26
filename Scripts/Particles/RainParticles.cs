using Godot;
using MyEnums;

public partial class RainParticles : GpuParticles3D
{
	private SceneResources _sceneResources;
	public override void _Ready()
	{
		// _sceneResources = SceneResources.Instance;

		// bool isRaining = _sceneResources.Weather == Weather.Rainy;
		// Emitting = isRaining;
		// Visible = isRaining;
	}
}
