using Godot;
using MyEnums;

public partial class UpgradeBtn : Button
{
	[Export] public UpgradeType Upgrade { get; set; }
	private SceneResources _sceneResources;
	private Signals _signals;

	public override void _Ready()
	{
		// _models = AssetServer.Instance.Models;
		// _label = GetNode<Label>("Label");
		_sceneResources = SceneResources.Instance;
		_signals = Signals.Instance;

		_signals.UpdateUpgradesAvailability += EnableDisableBtns;
		// MouseEntered += OnBtnEnter;
		// MouseExited += OnBtnExit;
		// Pressed += OnUnitSelect;
	}

	private void EnableDisableBtns()
	{
		Disabled = !_sceneResources.UpgradeAvailability[Upgrade];

		if (!Disabled)
			Text = Upgrade.ToString();
	}
}
