using Godot;
using MyEnums;

public partial class InfantryBtn : Button
{
	[Export] public InfantryType Unit { get; set; }
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

		if (_lockTexture == null) Utils.PrintErr("LockTexture not found for unit: " + Unit.ToString());
		if (_label == null) Utils.PrintErr("Label not found for unit: " + Unit.ToString());

		_signals.UpdateInfantryAvailability += EnableDisableBtns;
		MouseEntered += OnMouseEnter;
		MouseExited += OnMouseExit;
		Pressed += OnUnitSelect;
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;

		_label.Text = Unit.ToString();
	}

	private void OnUnitSelect()
	{
		var unit = _models.Infantry[Unit];
		Unit unitInstance = unit.Instantiate<Unit>();

		bool enoughFunds = _sceneResources.Funds >= unitInstance.Cost;
		if (!enoughFunds)
		{
			GD.Print("Not enough funds!");
			return;
		}

		GD.Print("Building " + Unit.ToString());
		_signals.EmitUpdateFunds(-unitInstance.Cost);
	}

	private void OnMouseEnter()
	{
		if (!_models.Infantry.ContainsKey(Unit))
		{
			Utils.PrintErr("MyModels.cs -> Units dictionary does not contains key for InfantryType: " + Unit);
			return;
		}

		if (!Disabled)
			SelfModulate = _hoverModulate;

		var packed = _models.Infantry[Unit];
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
		Disabled = !_sceneResources.InfantryAvailability[Unit];
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_lockTexture.Visible = Disabled;
	}
}
