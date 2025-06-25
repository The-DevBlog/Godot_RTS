using Godot;
using MyEnums;

public partial class UnitBtn : Button
{
	[Export] public UnitType Unit { get; set; }
	private Signals _signals;
	private MyModels _models;
	private SceneResources _sceneResources;
	private UnitBase _unit;
	private Label _label;
	private TextureRect _lockTexture;
	private Color _normalModulate = new Color("#c8c8c8");
	private Color _hoverModulate = new Color("#ffffff");
	// private Color _disabledModulate = new Color("#808080");
	private Color _disabledModulate = new Color("#262626");
	public override void _Ready()
	{
		_models = AssetServer.Instance.Models;
		_label = GetNode<Label>("Label");
		_lockTexture = GetNode<TextureRect>("LockTexture");
		_sceneResources = SceneResources.Instance;
		_signals = Signals.Instance;

		if (_lockTexture == null) Utils.PrintErr("LockTexture not found for unit: " + Unit.ToString());
		if (_label == null) Utils.PrintErr("Label not found for unit: " + Unit.ToString());

		_signals.UpdateUnitAvailability += EnableDisableBtns;
		MouseEntered += OnMouseEnter;
		MouseExited += OnMouseExit;
		Pressed += OnUnitSelect;
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;

		_label.Text = Unit.ToString();
	}

	private void OnUnitSelect()
	{
		GD.Print("Building " + Unit.ToString());

		var unit = _models.Units[Unit];
		UnitBase unitInstance = unit.Instantiate<UnitBase>();

		bool enoughFunds = _sceneResources.Funds >= unitInstance.Cost;
		if (!enoughFunds)
		{
			GD.Print("Not enough funds!");
			return;
		}

		_signals.EmitUpdateFunds(-unitInstance.Cost);
	}

	private void OnMouseEnter()
	{
		if (!_models.Units.ContainsKey(Unit))
		{
			Utils.PrintErr("MyModels.cs -> Units dictionary does not contains key for UnitType: " + Unit);
			return;
		}

		if (!Disabled)
			SelfModulate = _hoverModulate;

		var packed = _models.Units[Unit];
		var unit = packed.Instantiate<UnitBase>();

		_signals.EmitBuildOptionsBtnBtnHover(null, unit);
	}

	private void OnMouseExit()
	{
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_signals.EmitBuildOptionsBtnBtnHover(null, null);
	}

	private void EnableDisableBtns()
	{
		Disabled = !_sceneResources.UnitAvailability[Unit];
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_lockTexture.Visible = Disabled;
	}
}
