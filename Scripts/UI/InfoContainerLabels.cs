using Godot;

public partial class InfoContainerLabels : HBoxContainer
{
	[Export] public Label EnergyLabel { get; set; }
	[Export] public Label FundsLabel { get; set; }
	private SceneResources _sceneResources;
	private Signals _signals;
	private int _energyConsumed;

	public override void _Ready()
	{
		_sceneResources = SceneResources.Instance;

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
		EnergyLabel.Text = $"{_sceneResources.EnergyConsumed}/{_sceneResources.Energy}";
	}

	private void OnUpdateFunds()
	{
		FundsLabel.Text = $"${_sceneResources.Funds}";
	}
}
