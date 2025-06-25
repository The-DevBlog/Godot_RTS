using Godot;
using MyEnums;

public partial class UpgradeBtn : Button
{
	[Export] public UpgradeType Upgrade { get; set; }
	private SceneResources _sceneResources;
	private Signals _signals;
	private Color _normalModulate = new Color("#c8c8c8");
	private Color _hoverModulate = new Color("#ffffff");
	private Color _disabledModulate = new Color("#262626");
	private Label _label;
	private TextureRect _lockTexture;
	public override void _Ready()
	{
		if (Upgrade == UpgradeType.None)
			Utils.PrintErr("Upgrade type is set to none");

		_label = GetNode<Label>("Label");
		_lockTexture = GetNode<TextureRect>("LockTexture");
		_sceneResources = SceneResources.Instance;

		if (_lockTexture == null) Utils.PrintErr("LockTexture not found for unit: " + Upgrade.ToString());
		if (_label == null) Utils.PrintErr("Label not found for unit: " + Upgrade.ToString());


		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_label.Text = Upgrade.ToString();

		MouseEntered += OnMouseEnter;
		MouseExited += OnMouseExit;
		_signals = Signals.Instance;
		_signals.UpdateUpgradesAvailability += EnableDisableBtns;
	}

	private void OnMouseEnter()
	{
		// if (!_models.Units.ContainsKey(Unit))
		// {
		// 	Utils.PrintErr("MyModels.cs -> Units dictionary does not contains key for UnitType: " + Unit);
		// 	return;
		// }

		if (!Disabled)
			SelfModulate = _hoverModulate;

		// var packed = _models.Units[Unit];
		// var unit = packed.Instantiate<UnitBase>();

		// _signals.EmitBuildOptionsBtnBtnHover(null, unit);
	}

	private void OnMouseExit()
	{
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_signals.EmitBuildOptionsBtnBtnHover(null, null);
	}

	private void EnableDisableBtns()
	{
		Disabled = !_sceneResources.UpgradesAvailable;
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_lockTexture.Visible = Disabled;


	}
}
