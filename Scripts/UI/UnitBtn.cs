using Godot;
using MyEnums;

public partial class UnitBtn : Button
{
	[Export] public UnitType Unit { get; set; }
	private Resources _resources;
	private Signals _signals;
	private MyModels _models;
	private UnitBase _unit;

	public override void _Ready()
	{
		_models = AssetServer.Instance.Models;
		_resources = Resources.Instance;
		_signals = Signals.Instance;

		MouseEntered += OnBtnEnter;
		MouseExited += OnBtnExit;
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
