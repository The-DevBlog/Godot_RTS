using Godot;

public partial class HealthSystem : Node
{
	[Export] private Unit _unit;
	[Export] private Node3D _healthbar;
	private int _currentHP;
	private int _maxHP;
	private CombatSystem _combatSystem;

	public override void _Ready()
	{
		Utils.NullExportCheck(_unit);
		Utils.NullExportCheck(_healthbar);

		_combatSystem = GetNode<CombatSystem>("../CombatSystem");
		Utils.NullCheck(_combatSystem);

		_combatSystem.OnAttack += TakeDamage;
	}

	private void TakeDamage(Unit target, int dmg)
	{
		ProgressBar progressBar = target.GetNode<ProgressBar>("Healthbar/SubViewport/ProgressBar");
		Utils.NullCheck(progressBar);

		GD.Print($"Unit {target.Name} took {dmg} damage from {_unit.Name}.");

		target.CurrentHP -= dmg;

		progressBar.Value = (float)target.CurrentHP / target.HP * progressBar.MaxValue;

		// Check for death
		if (target.CurrentHP <= 0)
		{
			GD.Print($"{target.Name} has been destroyed!");
			target.QueueFree();
		}
	}
}
