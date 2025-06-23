using Godot;
using MyEnums;

public partial class UnitBtn : Button
{
	[Export] public UnitType Unit { get; set; }
	private GlobalResources _globalResources;
	private Signals _signals;
	private MyModels _models;
	private SceneResources _sceneResources;
	private UnitBase _unit;
	public override void _Ready()
	{
		_models = AssetServer.Instance.Models;
		_sceneResources = SceneResources.Instance;
		_globalResources = GlobalResources.Instance;
		_signals = Signals.Instance;

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


		GD.Print("Unit Instance: " + unitInstance);
	}

	private void OnBtnEnter()
	{
		var packed = _models.Units[Unit];
		var unit = packed.Instantiate<UnitBase>();

		_signals.EmitBuildOptionsBtnBtnHover(null, unit);
	}

	private void OnBtnExit()
	{
		_signals.EmitBuildOptionsBtnBtnHover(null, null);
	}
}
