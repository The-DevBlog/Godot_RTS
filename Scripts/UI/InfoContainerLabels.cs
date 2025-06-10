using Godot;

public partial class InfoContainerLabels : HBoxContainer
{
	[Export] public Label EnergyLabel { get; set; }
	[Export] public Label FundsLabel { get; set; }
	private Resources _resources;
	private Signals _signals;
	private int _energyConsumed;

	public override void _Ready()
	{
		_resources = Resources.Instance;

		Utils.NullExportCheck(EnergyLabel);
		Utils.NullExportCheck(FundsLabel);

		if (FundsLabel != null)
			OnUpdateFunds();

		_signals = Signals.Instance;
		_signals.UpdateEnergy += UpdateEnergy;
		_signals.UpdateFunds += OnUpdateFunds;
	}

	private void UpdateEnergy()
	{
		EnergyLabel.Text = $"{_resources.EnergyConsumed}/{_resources.Energy}";
	}

	private void OnUpdateFunds()
	{
		FundsLabel.Text = $"${_resources.Funds}";
	}
}
