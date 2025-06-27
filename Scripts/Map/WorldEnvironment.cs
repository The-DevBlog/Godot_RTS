using Godot;
using MyEnums;

public partial class WorldEnvironment : Godot.WorldEnvironment
{
	[Export] private MeshInstance3D _groundMesh;
	[Export] private GpuParticles3D _rainParticles;
	[Export] private GpuParticles3D _snowParticles;
	[Export] private ShaderMaterial _snowMaterialPartial;
	private SceneResources _sceneResources;
	private Weather _weather;
	private TimeOfDay _timeOfDay;
	private Season _season;
	private DirectionalLight3D _sunLight;
	// private Color _colorGroundSnow = new Color("#d0d0d0");
	private Color _colorGround = new Color("#547c53");
	private Color _colorNight = new Color("#7da8ff");
	private Color _colorDay = new Color("#e1ebff");

	public override void _Ready()
	{
		_sceneResources = SceneResources.Instance;

		_season = _sceneResources.Season;
		_weather = _sceneResources.Weather;
		_timeOfDay = _sceneResources.TimeOfDay;
		_sunLight = GetNode<DirectionalLight3D>("../DirectionalLight3D");

		Utils.NullCheck(_season);
		Utils.NullCheck(_sunLight);
		Utils.NullCheck(_weather);
		Utils.NullCheck(_timeOfDay);
		Utils.NullExportCheck(_rainParticles);
		Utils.NullExportCheck(_snowParticles);
		Utils.NullExportCheck(_groundMesh);

		// 1) Create a new StandardMaterial3D and set its BaseColor:
		// var groundMat = new StandardMaterial3D();
		// groundMat.AlbedoColor = _colorGround;

		// // 2) Assign it as an override on the MeshInstance3D:
		// _groundMesh.MaterialOverride = groundMat;

		InitSeason();
		InitTimeOfDay();
		InitWeather();
	}

	private void InitSeason()
	{
		var groundMaterial = new StandardMaterial3D();


		if (_season == Season.Summer)
		{
			groundMaterial.AlbedoColor = _colorGround;
			_groundMesh.MaterialOverride = groundMaterial;
		}
		else if (_season == Season.Winter)
		{
			Utils.NullExportCheck(_snowMaterialPartial);
			_groundMesh.MaterialOverride = _snowMaterialPartial;
			// groundMaterial.AlbedoColor = _colorGroundSnow;
		}
	}

	private void InitTimeOfDay()
	{
		if (_timeOfDay == TimeOfDay.Day)
		{
			_sunLight.LightEnergy = 1.75f;
			_sunLight.LightColor = _colorDay;
		}
		else if (_timeOfDay == TimeOfDay.Night)
		{
			_sunLight.LightEnergy = 1.0f;
			_sunLight.LightColor = _colorNight;
		}
	}

	private void InitWeather()
	{
		if (_weather == Weather.Rainy)
		{
			_rainParticles.Emitting = true;
			_rainParticles.Visible = true;
			Environment.VolumetricFogEnabled = true;
			Environment.VolumetricFogDensity = 0.01f;
		}
		else if (_weather == Weather.Snowy)
		{
			_snowParticles.Emitting = true;
			_snowParticles.Visible = true;
			Environment.VolumetricFogEnabled = true;
			Environment.VolumetricFogDensity = 0.015f;
		}
		else if (_weather == Weather.Sunny)
		{
			Environment.VolumetricFogEnabled = false;
		}
	}
}
