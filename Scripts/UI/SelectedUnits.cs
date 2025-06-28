using Godot;

public partial class SelectedUnits : MarginContainer
{
	[Export] private Container _spacer;
	private Signals _signals;

	public override void _Ready()
	{
		_signals = Signals.Instance;

		_signals.SelectUnits += ToggleVisibility;

		Utils.NullExportCheck(_spacer);
	}

	private void ToggleVisibility(Unit[] units)
	{
		bool visible = units != null && units.Length > 0;

		Visible = visible;
		_spacer.Visible = !visible;
	}
}
