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

		_currentHP = _unit.HP;
		_maxHP = _unit.HP;
		_combatSystem = GetNode<CombatSystem>("../CombatSystem");

		Utils.NullCheck(_combatSystem);

		_combatSystem.OnAttack += TakeDamage;
	}

	private void TakeDamage(Unit target, int dmg)
	{
		ProgressBar progressBar = target.GetNode<ProgressBar>("Healthbar/SubViewport/ProgressBar");
		Utils.NullCheck(progressBar);

		GD.Print($"Unit {target.Name} took {dmg} damage from {_unit.Name}.");

		_currentHP -= dmg;
		progressBar.Value = (float)_currentHP / _maxHP * progressBar.MaxValue;
	}
}
