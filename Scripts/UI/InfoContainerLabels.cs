using Godot;

public partial class InfoContainerLabels : HBoxContainer
{
	[Export] public Label EnergyLabel { get; set; }
	[Export] public Label FundsLabel { get; set; }

	// how fast the displayed funds move (units per second)
	[Export] public float FundsAnimSpeed = 600.0f;

	private TeamResources _sceneResources;
	private Signals _signals;

	// the funds value we're currently showing
	private int _displayFunds;
	// the actual funds we want to get to
	private int _targetFunds;

	public override void _Ready()
	{
		_sceneResources = TeamResources.Instance;

		Utils.NullExportCheck(EnergyLabel);
		Utils.NullExportCheck(FundsLabel);

		// initialize both display & target to the starting scene funds
		_displayFunds = _sceneResources.Funds;
		_targetFunds = _displayFunds;
		FundsLabel.Text = $"${_displayFunds}";

		_signals = Signals.Instance;
		_signals.UpdateEnergy += UpdateEnergy;
		_signals.UpdateFunds += OnUpdateFunds;
	}

	public override void _Process(double delta)
	{
		if (_displayFunds == _targetFunds)
			return;

		// step _displayFunds toward the target at FundsAnimSpeed units/sec
		int step = (int)(FundsAnimSpeed * delta);
		_displayFunds = (int)Mathf.MoveToward(_displayFunds, _targetFunds, step);

		FundsLabel.Text = $"${_displayFunds}";
	}

	private void UpdateEnergy()
	{
		EnergyLabel.Text = $"{_sceneResources.EnergyConsumed}/{_sceneResources.Energy}";
	}

	private void OnUpdateFunds()
	{
		// whenever the real funds change, update the target
		_targetFunds = _sceneResources.Funds;
		// (the Process loop will animate _displayFunds → _targetFunds)
	}
}
