using Godot;

public partial class HealthSystem : Node
{
	[Export] private Unit _unit;
	private ProgressBar _healthbar;
	private int _currentHP;
	private int _maxHP;
	private Color _healthbarColorRed;
	private Color _healthbarColorGreen;
	private Color _healthbarColorOrange;

	public override void _Ready()
	{
		Utils.NullExportCheck(_unit);

		_healthbar = GetNode<ProgressBar>("../../Healthbar/SubViewport/ProgressBar");
		Utils.NullCheck(_healthbar);

		_healthbarColorGreen = new Color("#5cc154");
		_healthbarColorRed = new Color("#ff0000");
		_healthbarColorOrange = new Color("#ff8c00");
	}

	// IDamageable implementation
	public void ApplyDamage(int amount, Vector3 hitPos, Vector3 hitNormal)
	{
		_unit.CurrentHP = Mathf.Max(0, _unit.CurrentHP - amount);

		// Move the bar
		_healthbar.Value = (float)_unit.CurrentHP / _unit.HP * _healthbar.MaxValue;

		// Color
		UpdateHealthbar(_unit, _healthbar);

		if (_unit.CurrentHP <= 0)
			Destroy();
	}

	private void Destroy()
	{
		var deathAudio = _unit.GetNode<AudioStreamPlayer3D>("Audio/Death");
		deathAudio.Reparent(_unit.GetParent());
		deathAudio.Play();

		// TODO: Check if these animations are still in the scene!
		_unit.Death.Reparent(_unit.GetParent());
		AnimationPlayer deathAnimation = _unit.Death.GetNode<AnimationPlayer>("AnimationPlayer");
		deathAnimation.Play(MyEnums.Animations.Death.ToString());

		_unit.QueueFree();
	}

	private void UpdateHealthbar(Unit unit, ProgressBar bar)
	{
		float healthPercent = (float)unit.CurrentHP / unit.HP;

		if (bar.GetThemeStylebox("fill") is StyleBoxFlat flat)
		{
			var copy = (StyleBoxFlat)flat.Duplicate(true);

			if (healthPercent < 0.35f)
				copy.BgColor = _healthbarColorRed;
			else if (healthPercent < 0.60f)
				copy.BgColor = _healthbarColorOrange;
			else
				copy.BgColor = _healthbarColorGreen;

			bar.AddThemeStyleboxOverride("fill", copy);
		}
	}
}
