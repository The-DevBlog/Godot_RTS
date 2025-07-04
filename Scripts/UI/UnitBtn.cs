using Godot;
using MyEnums;

public partial class UnitBtn : Button
{
	[Export] public InfantryType Infantry { get; set; }
	private Signals _signals;
	private MyModels _models;
	private Player _player;
	private Unit _unit;
	private TextureRect _lockTexture;
	private Color _normalModulate = new Color("#c8c8c8");
	private Color _hoverModulate = new Color("#ffffff");
	private Color _disabledModulate = new Color("#737373");
	public override void _Ready()
	{
		_models = AssetServer.Instance.Models;
		_lockTexture = GetNode<TextureRect>("LockTexture");
		_player = PlayerManager.Instance.LocalPlayer;
		_signals = Signals.Instance;

		if (Infantry == InfantryType.None) Utils.PrintErr("InfantryType is to set None");
		if (_lockTexture == null) Utils.PrintErr("LockTexture not found for unit: " + Infantry.ToString());

		_player.UpdateInfantryAvailability += EnableDisableBtns;
		MouseEntered += OnMouseEnter;
		MouseExited += OnMouseExit;
		Pressed += OnUnitSelect;
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
	}

	private void OnUnitSelect()
	{
		var unit = _models.Infantry[Infantry];
		Infantry infantryInstance = unit.Instantiate<Infantry>();

		bool enoughFunds = _player.Funds >= infantryInstance.Cost;
		if (!enoughFunds)
		{
			GD.Print("Not enough funds!");
			return;
		}

		_player.EmitBuildInfantry(infantryInstance);
		_player.UpdateFunds(-infantryInstance.Cost);
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
		Disabled = !_player.InfantryAvailability[Infantry];
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_lockTexture.Visible = Disabled;
	}
}
