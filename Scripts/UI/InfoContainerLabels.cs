using Godot;

public partial class InfoContainerLabels : HBoxContainer
{
	[Export] public Label EnergyLabel { get; set; }
	private Resources _resources;
	private Signals _signals;
	private int _energyConsumed;

	public override void _Ready()
	{
		if (EnergyLabel == null)
			Utils.PrintErr("EneryLabel is not set!");

		_resources = Resources.Instance;
		_signals = Signals.Instance;
		_signals.UpdateEnergy += OnUpdateEnergy;
	}

	private void OnUpdateEnergy(int energy)
	{
		EnergyLabel.Text = $"{_energyConsumed}/{_resources.Energy}";
	}
}
