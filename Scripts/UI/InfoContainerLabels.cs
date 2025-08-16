using Godot;

public partial class InfoContainerLabels : HBoxContainer
{
	[Export] public Label EnergyLabel { get; set; }
	[Export] public Label FundsLabel { get; set; }
	[Export] public float FundsAnimationSpeed = 600.0f;
	private Player _player;
	private Signals _signals;
	private float _displayFundsFloat; // funds used for calculating the display value. Float is used for smooth animation
	private int _displayFunds; // the funds value we're currently showing
	private int _targetFunds; // the actual funds we want to get to 

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
		_displayFundsFloat = _player.Funds;
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

		_displayFundsFloat = Mathf.MoveToward(_displayFundsFloat, _targetFunds, FundsAnimationSpeed * (float)delta);
		int newDisplay = Mathf.RoundToInt(_displayFundsFloat);

		if (newDisplay != _displayFunds)
		{
			_displayFunds = newDisplay;
			FundsLabel.Text = $"${_displayFunds}";
		}
	}

	private void UpdateEnergy(int amount)
	{
		GD.Print("Updating energy label to " + _player.EnergyConsumed + "/" + _player.Energy);
		EnergyLabel.Text = $"{_player.EnergyConsumed}/{_player.Energy}";
	}

	private void UpdateFunds(int amount)
	{
		GD.Print("Updating funds label to " + _player.Funds);
		_targetFunds = _player.Funds;
	}
}
