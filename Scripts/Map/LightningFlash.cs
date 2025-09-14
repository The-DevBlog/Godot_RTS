using Godot;
using System.Threading.Tasks;

public partial class LightningFlash : CanvasLayer
{

	// --- Flash tuning ---
	[Export] public float PeakAlpha = 0.95f;                 // brightness of the flash
	[Export] public Vector2 BurstCountRange = new(1, 3);     // pops per lightning event
	[Export] public Vector2 RiseTimeRange = new(0.03f, 0.08f);
	[Export] public Vector2 FallTimeRange = new(0.08f, 0.18f);
	[Export] public Vector2 PauseRange = new(0.05f, 0.18f); // gap between pops

	// --- Random interval between lightning events ---
	[Export] public Vector2 EventIntervalRange = new(3f, 12f);
	[Export] public bool AutoStart = true;
	[Export] public bool FlashDuringPause = false;           // keep flashing when the game is paused

	// --- Optional light punch (drag your DirectionalLight3D / "Sun" here) ---
	[Export] public DirectionalLight3D LightningLight;
	[Export] public float LightBoost = 2.0f;

	private GpuParticles3D _lightningParticles;
	private AudioStreamPlayer3D _thunderSound;
	private ColorRect _overlay;
	private readonly RandomNumberGenerator _rng = new();
	private bool _autoRunning;

	public override async void _Ready()
	{
		Vector2 mapSize = GlobalResources.Instance.MapSize;
		Vector3 emissionBox = new Vector3(mapSize.X / 2, 1, mapSize.Y / 2);

		// Find the overlay and make it full-screen
		_overlay = GetNodeOrNull<ColorRect>("ColorRect");
		Utils.NullCheck(_overlay);

		_thunderSound = GetNodeOrNull<AudioStreamPlayer3D>("AudioStreamPlayer3D");
		Utils.NullCheck(_thunderSound);

		_lightningParticles = GetNodeOrNull<GpuParticles3D>("GPUParticles3D");
		Utils.NullCheck(_lightningParticles);

		if (_lightningParticles.ProcessMaterial is not ParticleProcessMaterial mat)
		{
			mat = new ParticleProcessMaterial();
			_lightningParticles.ProcessMaterial = mat;
		}

		mat.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box;
		mat.EmissionBoxExtents = emissionBox;

		Layer = 200; // draw on top of other UI
		_overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_overlay.OffsetLeft = _overlay.OffsetTop = 0;
		_overlay.OffsetRight = _overlay.OffsetBottom = 0;
		_overlay.MouseFilter = Control.MouseFilterEnum.Ignore;

		// IMPORTANT: only one alpha is 0; the other stays 1 (avoid double-alpha = invisible)
		_overlay.Color = new Color(1, 1, 1, 1); // fully opaque base color
		_overlay.Modulate = new Color(1, 1, 1, 0); // hidden via modulate alpha

		_rng.Randomize();

		// Ensure we start after first frame
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		if (AutoStart) StartAuto();
	}

	// --- Public controls ---
	public void StartAuto()
	{
		if (_autoRunning) return;
		_autoRunning = true;
		_ = AutoLoopAsync();
	}
	public void StopAuto() => _autoRunning = false;

	// Random lightning events loop
	private async Task AutoLoopAsync()
	{
		while (_autoRunning && IsInsideTree())
		{
			float wait = _rng.RandfRange(EventIntervalRange.X, EventIntervalRange.Y);
			var waitTimer = GetTree().CreateTimer(wait, processAlways: FlashDuringPause);
			await ToSignal(waitTimer, SceneTreeTimer.SignalName.Timeout);
			if (!_autoRunning || !IsInsideTree()) break;

			int pops = Mathf.RoundToInt(_rng.RandfRange(BurstCountRange.X, BurstCountRange.Y));

			// (Optional) realistic delay before thunder
			var delay = _rng.RandfRange(0.2f, 1.0f);
			var delayTimer = GetTree().CreateTimer(delay, processAlways: FlashDuringPause);
			await ToSignal(delayTimer, SceneTreeTimer.SignalName.Timeout);

			PlayThunderOnce(); // ← play only once per event

			for (int i = 0; i < pops; i++)
			{
				float rise = _rng.RandfRange(RiseTimeRange.X, RiseTimeRange.Y);
				float fall = _rng.RandfRange(FallTimeRange.X, FallTimeRange.Y);
				await FlashOnceAsync(rise, fall); // ← no Play() here

				if (i < pops - 1)
				{
					float gap = _rng.RandfRange(PauseRange.X, PauseRange.Y);
					var gapTimer = GetTree().CreateTimer(gap, processAlways: FlashDuringPause);
					await ToSignal(gapTimer, SceneTreeTimer.SignalName.Timeout);
				}
			}
		}
	}

	// One flash (white pop + decay). Safe to call manually, too.
	public async Task FlashOnceAsync(float rise, float fall)
	{
		if (_overlay == null) return;

		_lightningParticles?.Restart();

		float baseLight = LightningLight?.LightEnergy ?? 0f;

		// Start two tweens at once (parallel): overlay + optional light
		var overlayTween = GetTree().CreateTween();
		overlayTween.TweenProperty(_overlay, "modulate:a", PeakAlpha, rise)
					.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		overlayTween.TweenProperty(_overlay, "modulate:a", 0.0f, fall)
					.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);

		Tween lightTween = null;
		if (LightningLight != null)
		{
			lightTween = GetTree().CreateTween();
			lightTween.TweenProperty(LightningLight, "light_energy", baseLight + LightBoost, rise)
					  .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			lightTween.TweenProperty(LightningLight, "light_energy", baseLight, fall)
					  .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		}

		await ToSignal(overlayTween, Tween.SignalName.Finished);
		if (lightTween != null) await ToSignal(lightTween, Tween.SignalName.Finished);
	}

	private void PlayThunderOnce()
	{
		if (_thunderSound == null) return;
		if (!_thunderSound.Playing)
			_thunderSound.Play();
	}
}
