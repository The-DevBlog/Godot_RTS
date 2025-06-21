using Godot;

public partial class WorldEnvironment : Godot.WorldEnvironment
{
	public override void _Ready()
	{
		Environment.VolumetricFogEnabled = SceneResources.Instance.RainyWeather;
	}
}
