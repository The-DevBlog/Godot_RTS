using Godot;

public partial class InfoContainerLabels : HBoxContainer
{
	[Export] public Label EnergyLabel { get; set; }
	[Export] public Label FundsLabel { get; set; }

	// how fast the displayed funds move (units per second)
	[Export] public float FundsAnimSpeed = 600.0f;

	private Player _player;
	private Signals _signals;

	// the funds value we're currently showing
	private int _displayFunds;
	// the actual funds we want to get to
	private int _targetFunds;

	public override void _Ready()
	{
		PlayerManager.Instance.WhenHumanPlayerReady(player =>
		{
			_player = player;
		});

		Utils.NullExportCheck(EnergyLabel);
		Utils.NullExportCheck(FundsLabel);

		// initialize both display & target to the starting scene funds
		_displayFunds = _player.Funds;
		_targetFunds = _displayFunds;
		FundsLabel.Text = $"${_displayFunds}";

		_signals = Signals.Instance;
		_player.OnUpdateEnergy += UpdateEnergy;
		_player.OnUpdateFunds += UpdateFunds;
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

	private void UpdateEnergy(int amount)
	{
		GD.Print("Updating energy label to " + _player.EnergyConsumed + "/" + _player.Energy);
		EnergyLabel.Text = $"{_player.EnergyConsumed}/{_player.Energy}";
	}

	private void UpdateFunds(int amount)
	{
		GD.Print("Updating funds label to " + _player.Funds);
		// whenever the real funds change, update the target
		_targetFunds = _player.Funds;
		// (the Process loop will animate _displayFunds → _targetFunds)
	}
}
