using System.Collections.Generic;
using Godot;
using MyEnums;

public partial class WorldEnvironment : Godot.WorldEnvironment
{
	[Export] private MeshInstance3D _groundMesh;
	[Export] private GpuParticles3D _rainParticles;
	[Export] private GpuParticles3D _stormyRainParticles;
	[Export] private GpuParticles3D _snowParticles;
	[Export] private ShaderMaterial _snowMaterialPartial;
	private GlobalResources _globalResources;
	private Weather _weather;
	private TimeOfDay _timeOfDay;
	private Season _season;
	private DirectionalLight3D _sunLight;
	private Color _colorGround = new Color("#547c53");
	private Color _colorNight = new Color("#7da8ff");
	private Color _colorDay = new Color("#e1ebff");
	private Color _colorDusk = new Color("#FFB380");

	public override void _Ready()
	{
		_globalResources = GlobalResources.Instance;

		_season = _globalResources.Season;
		_weather = _globalResources.Weather;
		_timeOfDay = _globalResources.TimeOfDay;
		_sunLight = GetNode<DirectionalLight3D>("../DirectionalLight3D");

		Utils.NullCheck(_season);
		Utils.NullCheck(_sunLight);
		Utils.NullCheck(_weather);
		Utils.NullCheck(_timeOfDay);
		Utils.NullExportCheck(_rainParticles);
		Utils.NullExportCheck(_stormyRainParticles);
		Utils.NullExportCheck(_snowParticles);
		Utils.NullExportCheck(_groundMesh);

		InitSeason();
		InitTimeOfDay();
		InitWeather();
	}

	private void InitSeason()
	{
		if (_season == Season.Summer)
		{
			var groundMaterial = new StandardMaterial3D();
			groundMaterial.AlbedoColor = _colorGround;
			_groundMesh.MaterialOverride = groundMaterial;
		}
		else if (_season == Season.Winter)
		{
			Utils.NullExportCheck(_snowMaterialPartial);
			_groundMesh.MaterialOverride = _snowMaterialPartial;
		}
	}

	private void InitTimeOfDay()
	{
		Vector3 sunRotation = Vector3.Zero;
		if (_timeOfDay == TimeOfDay.Day)
		{
			sunRotation.X = Mathf.DegToRad(33);
			_sunLight.LightEnergy = 1.75f;
			_sunLight.LightColor = _colorDay;
		}
		else if (_timeOfDay == TimeOfDay.Night)
		{
			ApplyShadows(isNight: true);
			sunRotation.X = Mathf.DegToRad(33);
			_sunLight.LightEnergy = 1.0f;
			_sunLight.LightColor = _colorNight;
		}
		else if (_timeOfDay == TimeOfDay.Dusk)
		{
			sunRotation.X = Mathf.DegToRad(-150);
			sunRotation.Y = Mathf.DegToRad(110);
			_sunLight.LightEnergy = 1.75f;
			_sunLight.Rotation = sunRotation;
			_sunLight.LightColor = _colorDusk;
		}
	}

	private void InitWeather()
	{
		if (_weather == Weather.Rainy)
		{
			_rainParticles.Emitting = true;
			_rainParticles.Visible = true;
			Environment.VolumetricFogEnabled = true;
			Environment.VolumetricFogDensity = 0.0075f;
		}
		if (_weather == Weather.Stormy)
		{
			var lightningPs = AssetServer.Instance.Models.WeatherEffects[Weather.Stormy];
			var lightning = lightningPs.Instantiate<LightningFlash>();
			if (lightning == null)
			{
				Utils.PrintErr("Stormy lightning scene root isn't LightningFlash.");
				return;
			}

			// Add to the current scene root *after* the current frame to ensure order.
			// (Avoids edge cases if you're still inside _Ready of another node.)
			GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, lightning);

			// Optionally force autostart in case the scene’s exported flag is false
			lightning.AutoStart = true;

			_stormyRainParticles.Emitting = true;
			_stormyRainParticles.Visible = true;
			Environment.VolumetricFogEnabled = true;
			Environment.VolumetricFogDensity = 0.01f;
		}
		else if (_weather == Weather.Snowy)
		{
			_snowParticles.Emitting = true;
			_snowParticles.Visible = true;
			Environment.VolumetricFogEnabled = true;
			Environment.VolumetricFogDensity = 0.012f;
		}
		else if (_weather == Weather.Sunny)
		{
			Environment.VolumetricFogEnabled = false;
		}
	}

	// Apply shadows based on night/day
	private void ApplyShadows(bool isNight)
	{
		var root = GetTree().CurrentScene;
		if (root == null) return;

		// 1) Toggle shadows on ALL lights (Directional/Omni/Spot)
		foreach (var n in Enumerate<Light3D>(root))
			n.ShadowEnabled = !isNight;

		// 2) OPTIONAL: also stop all meshes from casting shadows (extra perf win)
		// Comment this out if you still want, e.g., vehicle headlights to cast shadows at night.
		foreach (var g in Enumerate<GeometryInstance3D>(root))
			g.CastShadow = isNight
				? GeometryInstance3D.ShadowCastingSetting.Off
				: GeometryInstance3D.ShadowCastingSetting.On;
	}

	private static IEnumerable<T> Enumerate<T>(Node root) where T : class
	{
		var stack = new Stack<Node>();
		stack.Push(root);
		while (stack.Count > 0)
		{
			var n = stack.Pop();
			if (n is T t) yield return t;
			foreach (Node c in n.GetChildren()) stack.Push(c);
		}
	}

}
