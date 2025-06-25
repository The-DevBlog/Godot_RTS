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
	public override void _Ready()
	{
		_models = AssetServer.Instance.Models;
		_label = GetNode<Label>("Label");
		_sceneResources = SceneResources.Instance;
		_signals = Signals.Instance;

		_signals.UpdateUnitAvailability += EnableDisableBtns;
		MouseEntered += OnBtnEnter;
		MouseExited += OnBtnExit;
		Pressed += OnUnitSelect;
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

	private void OnBtnEnter()
	{
		if (!_models.Units.ContainsKey(Unit))
		{
			Utils.PrintErr("MyModels.cs -> Units dictionary does not contains key for UnitType: " + Unit);
			return;
		}

		var packed = _models.Units[Unit];
		var unit = packed.Instantiate<UnitBase>();

		_signals.EmitBuildOptionsBtnBtnHover(null, unit);
	}

	private void OnBtnExit()
	{
		_signals.EmitBuildOptionsBtnBtnHover(null, null);
	}

	private void EnableDisableBtns()
	{
		Disabled = !_sceneResources.UnitAvailability[Unit];

		if (!Disabled)
			_label.Text = Unit.ToString();
	}
}
