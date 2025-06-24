using Godot;
using MyEnums;

public partial class UpgradeBtn : Button
{
	[Export] public UpgradeType Upgrade { get; set; }
	private SceneResources _sceneResources;
	private Signals _signals;

	public override void _Ready()
	{
		if (Upgrade == UpgradeType.None)
			Utils.PrintErr("Upgrade type is set to none");

		_sceneResources = SceneResources.Instance;

		_signals = Signals.Instance;
		_signals.UpdateUpgradesAvailability += EnableDisableBtns;
	}

	private void EnableDisableBtns()
	{
		Disabled = !_sceneResources.UpgradeAvailability[Upgrade];
	}
}
