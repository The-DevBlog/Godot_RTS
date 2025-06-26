using Godot;
using MyEnums;

public partial class UnitBtn : Button
{
	[Export] public InfantryType Infantry { get; set; }
	private Signals _signals;
	private MyModels _models;
	private SceneResources _sceneResources;
	private Unit _unit;
	private Label _label;
	private TextureRect _lockTexture;
	private Color _normalModulate = new Color("#c8c8c8");
	private Color _hoverModulate = new Color("#ffffff");
	private Color _disabledModulate = new Color("#262626");
	public override void _Ready()
	{
		_models = AssetServer.Instance.Models;
		_label = GetNode<Label>("Label");
		_lockTexture = GetNode<TextureRect>("LockTexture");
		_sceneResources = SceneResources.Instance;
		_signals = Signals.Instance;

		if (Infantry == InfantryType.None) Utils.PrintErr("InfantryType is to set None");
		if (_lockTexture == null) Utils.PrintErr("LockTexture not found for unit: " + Infantry.ToString());
		if (_label == null) Utils.PrintErr("Label not found for unit: " + Infantry.ToString());

		_signals.UpdateInfantryAvailability += EnableDisableBtns;
		MouseEntered += OnMouseEnter;
		MouseExited += OnMouseExit;
		Pressed += OnUnitSelect;
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;

		_label.Text = Infantry.ToString();
	}

	private void OnUnitSelect()
	{
		var unit = _models.Infantry[Infantry];
		Infantry infantryInstance = unit.Instantiate<Infantry>();

		bool enoughFunds = _sceneResources.Funds >= infantryInstance.Cost;
		if (!enoughFunds)
		{
			GD.Print("Not enough funds!");
			return;
		}

		_signals.EmitBuildInfantry(infantryInstance);
		_signals.EmitUpdateFunds(-infantryInstance.Cost);
	}


	private void OnMouseEnter()
	{
		if (!_models.Infantry.ContainsKey(Infantry))
		{
			Utils.PrintErr("MyModels.cs -> Units dictionary does not contains key for UnitType: " + Infantry);
			return;
		}

		if (!Disabled)
			SelfModulate = _hoverModulate;

		var packed = _models.Infantry[Infantry];
		var unit = packed.Instantiate<Unit>();

		_signals.EmitBuildOptionsBtnBtnHover(null, unit);
	}

	private void OnMouseExit()
	{
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_signals.EmitBuildOptionsBtnBtnHover(null, null);
	}

	private void EnableDisableBtns()
	{
		Disabled = !_sceneResources.InfantryAvailability[Infantry];
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_lockTexture.Visible = Disabled;
	}
}
