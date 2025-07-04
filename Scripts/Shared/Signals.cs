using Godot;
using MyEnums;

public partial class Signals : Node
{
	public static Signals Instance { get; private set; }
	[Signal] public delegate void UpdateNavigationMapEventHandler(NavigationRegion3D region);
	[Signal] public delegate void OnBuildOptionsBtnHoverEventHandler(StructureBase structure, Unit unit);
	[Signal] public delegate void OnUpgradeBtnHoverEventHandler(UpgradeType upgrade);
	[Signal] public delegate void UpdateEnergyColorEventHandler();

	public override void _Ready()
	{
		Instance = this;
	}

	public void EmitUpdateNavigationMap(NavigationRegion3D region) => EmitSignal(SignalName.UpdateNavigationMap, region);

	public void EmitBuildOptionsBtnBtnHover(StructureBase structure, Unit unit)
	{
		EmitSignal(SignalName.OnBuildOptionsBtnHover, structure, unit);
	}

	public void EmitUpgradeBtnHover(UpgradeType upgrade)
	{
		EmitSignal(SignalName.OnUpgradeBtnHover, (int)upgrade);
	}
}
