using Godot;
using MyEnums;

public partial class Signals : Node
{
	public static Signals Instance { get; private set; }
	[Signal] public delegate void UpdateNavigationMapEventHandler(NavigationRegion3D region);
	// [Signal] public delegate void DeselectAllUnitsEventHandler();
	// [Signal] public delegate void UpdateEnergyEventHandler();
	// [Signal] public delegate void UpdateFundsEventHandler();
	[Signal] public delegate void OnBuildOptionsBtnHoverEventHandler(StructureBase structure, Unit unit);
	[Signal] public delegate void OnUpgradeBtnHoverEventHandler(UpgradeType upgrade);
	// [Signal] public delegate void AddStructureEventHandler(int structureId);
	// [Signal] public delegate void SelectUnitsEventHandler(Unit[] units);
	// [Signal] public delegate void BuildVehicleEventHandler(Vehicle vehicle);
	// [Signal] public delegate void BuildInfantryEventHandler(Infantry infantry);
	[Signal] public delegate void UpdateEnergyColorEventHandler();
	// [Signal] public delegate void UpdateInfantryAvailabilityEventHandler();
	// [Signal] public delegate void UpdateVehicleAvailabilityEventHandler();
	// [Signal] public delegate void UpdateUpgradesAvailabilityEventHandler();
	// private PlayerManager _playerManager;

	public override void _Ready()
	{
		Instance = this;
		// _playerManager = PlayerManager.Instance;
	}

	public void EmitUpdateNavigationMap(NavigationRegion3D region) => EmitSignal(SignalName.UpdateNavigationMap, region);

	// public void EmitSelectUnits(Unit[] units)
	// {
	// 	GD.Print("Select Units: " + units.Length);
	// 	EmitSignal(SignalName.SelectUnits, units);
	// }

	// public void EmitUpdateEnergy(int playerId, int energy)
	// {
	// 	Player player = _playerManager.GetPlayer(playerId);

	// 	GD.Print(player.Name + ": " + energy);

	// 	if (energy > 0)
	// 		player.UpdateEnergy(energy);
	// 	else if (energy < 0)
	// 		player.UpdateEnergyConsumed(Math.Abs(energy));

	// 	if (player.EnergyConsumed > player.Energy)
	// 		EmitSignal(SignalName.UpdateEnergyColor);

	// 	EmitSignal(SignalName.UpdateEnergy);
	// }

	// public void EmitUpdateFunds(int playerId, int funds)
	// {
	// 	Player player = _playerManager.GetPlayer(playerId);

	// 	GD.Print(player.Name + ": " + funds);

	// 	player.UpdateFunds(funds);
	// 	EmitSignal(SignalName.UpdateFunds);
	// }

	// public void EmitAddStructure(int playerId, StructureBase structure)
	// {
	// 	Player player = _playerManager.GetPlayer(playerId);

	// 	GD.Print(player.Name + ": Add Structure: " + structure);

	// 	// player.AddStructure(structure);
	// 	EmitSignal(SignalName.AddStructure, (int)structure);

	// 	// if (structure == StructureType.Barracks)
	// 	// 	EmitUpdateInfantryAvailability();
	// 	// else if (structure == StructureType.Garage)
	// 	// 	EmitUpdateVehicleAvailability();
	// 	// else if (structure == StructureType.ResearchLab)
	// 	// 	EmitUpdateUpgradesAvailability();
	// }

	public void EmitBuildOptionsBtnBtnHover(StructureBase structure, Unit unit)
	{
		EmitSignal(SignalName.OnBuildOptionsBtnHover, structure, unit);
	}

	public void EmitUpgradeBtnHover(UpgradeType upgrade)
	{
		EmitSignal(SignalName.OnUpgradeBtnHover, (int)upgrade);
	}

	// public void EmitUpdateUpgradesAvailability()
	// {
	// 	EmitSignal(SignalName.UpdateUpgradesAvailability);
	// }

	// public void EmitUpdateVehicleAvailability()
	// {
	// 	EmitSignal(SignalName.UpdateVehicleAvailability);
	// }

	// public void EmitUpdateInfantryAvailability()
	// {
	// 	EmitSignal(SignalName.UpdateInfantryAvailability);
	// }

	// public void EmitBuildVehicle(Vehicle vehicle)
	// {
	// 	EmitSignal(SignalName.BuildVehicle, vehicle);
	// }

	// public void EmitBuildInfantry(Infantry infantry)
	// {
	// 	EmitSignal(SignalName.BuildInfantry, infantry);
	// }
}
