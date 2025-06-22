using Godot;

public partial class WorldEnvironment : Godot.WorldEnvironment
{
	private SceneResources _sceneResources;
	private DirectionalLight3D _sunLight;
	private Color _nightColor = new Color("#7da8ff");
	private Color _dayColor = new Color("#e1ebff");

	public override void _Ready()
	{
		_sceneResources = SceneResources.Instance;
		_sunLight = GetNode<DirectionalLight3D>("../DirectionalLight3D");

		Utils.NullCheck(_sunLight);

		RainyWeather();
		SunnyDay();
	}

	private void SunnyDay()
	{
		if (_sceneResources.RainyWeather)
			return;

		_sunLight.LightEnergy = 2.0f;
		_sunLight.LightColor = _dayColor;

		Environment.VolumetricFogEnabled = false;
	}

	private void RainyWeather()
	{
		if (!_sceneResources.RainyWeather)
			return;

		_sunLight.LightEnergy = 1;
		_sunLight.LightColor = _nightColor;

		Environment.VolumetricFogEnabled = true;
	}
}
