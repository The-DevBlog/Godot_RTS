using Godot;

public partial class HealthSystem : Node
{
	[Export] private Unit _unit;
	private ProgressBar _healthbar;
	private int _currentHP;
	private int _maxHP;
	private CombatSystem _combatSystem;
	private Color _healthbarColorRed;
	private Color _healthbarColorGreen;
	private Color _healthbarColorOrange;

	public override void _Ready()
	{
		Utils.NullExportCheck(_unit);

		_healthbar = GetNode<ProgressBar>("../../Healthbar/SubViewport/ProgressBar");
		Utils.NullCheck(_healthbar);

		_combatSystem = GetNode<CombatSystem>("../CombatSystem");
		Utils.NullCheck(_combatSystem);

		_healthbarColorGreen = new Color("#5cc154");
		_healthbarColorRed = new Color("#ff0000");
		_healthbarColorOrange = new Color("#ff8c00");

		_combatSystem.OnAttack += TakeDamage;
	}

	private void TakeDamage(Unit target, int dmg)
	{
		ProgressBar targetHealthbar = target.GetNode<ProgressBar>("Healthbar/SubViewport/ProgressBar");
		Utils.NullCheck(targetHealthbar);

		GD.Print($"Unit {target.Name} took {dmg} damage from {_unit.Name}.");

		target.CurrentHP -= dmg;

		targetHealthbar.Value = (float)target.CurrentHP / target.HP * targetHealthbar.MaxValue;

		// Check for death
		if (target.CurrentHP <= 0)
		{
			GD.Print($"{target.Name} has been destroyed!");
			target.QueueFree();
		}

		// update healthbar
		UpdateHealthbar(target, targetHealthbar);
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
