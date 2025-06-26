using System;
using Godot;
using MyEnums;

public partial class Signals : Node
{
	public static Signals Instance { get; private set; }
	[Signal] public delegate void UpdateNavigationMapEventHandler(NavigationRegion3D region);
	[Signal] public delegate void DeselectAllUnitsEventHandler();
	[Signal] public delegate void UpdateEnergyEventHandler();
	[Signal] public delegate void UpdateFundsEventHandler();
	[Signal] public delegate void OnBuildOptionsBtnHoverEventHandler(StructureBase structure, Unit unit);
	[Signal] public delegate void OnUpgradeBtnHoverEventHandler(UpgradeType upgrade);
	[Signal] public delegate void AddStructureEventHandler(int structureId);
	[Signal] public delegate void BuildVehicleEventHandler(Vehicle vehicle);
	[Signal] public delegate void BuildInfantryEventHandler(Infantry infantry);
	[Signal] public delegate void UpdateEnergyColorEventHandler();
	[Signal] public delegate void UpdateInfantryAvailabilityEventHandler();
	[Signal] public delegate void UpdateVehicleAvailabilityEventHandler();
	[Signal] public delegate void UpdateUpgradesAvailabilityEventHandler();
	private SceneResources _sceneResources;

	public override void _Ready()
	{
		Instance = this;
		_sceneResources = SceneResources.Instance;
	}

	public void EmitUpdateNavigationMap(NavigationRegion3D region) => EmitSignal(SignalName.UpdateNavigationMap, region);

	public void EmitUpdateEnergy(int energy)
	{
		GD.Print("Update Energy: " + energy);

		if (energy > 0)
			_sceneResources.Energy += energy;
		else if (energy < 0)
			_sceneResources.EnergyConsumed += Math.Abs(energy);

		if (_sceneResources.EnergyConsumed > _sceneResources.Energy)
			EmitSignal(SignalName.UpdateEnergyColor);

		EmitSignal(SignalName.UpdateEnergy);
	}

	public void EmitUpdateFunds(int funds)
	{
		GD.Print("Update Funds: " + funds);

		_sceneResources.Funds += funds;
		EmitSignal(SignalName.UpdateFunds);
	}

	public void EmitAddStructure(StructureType structure)
	{
		GD.Print("Add Structure: " + structure);

		_sceneResources.AddStructure(structure);
		EmitSignal(SignalName.AddStructure, (int)structure);

		if (structure == StructureType.Barracks)
			EmitUpdateInfantryAvailability();
		else if (structure == StructureType.Garage)
			EmitUpdateVehicleAvailability();
		else if (structure == StructureType.ResearchLab)
			EmitUpdateUpgradesAvailability();
	}

	public void EmitBuildOptionsBtnBtnHover(StructureBase structure, Unit unit)
	{
		EmitSignal(SignalName.OnBuildOptionsBtnHover, structure, unit);
	}

	public void EmitUpgradeBtnHover(UpgradeType upgrade)
	{
		EmitSignal(SignalName.OnUpgradeBtnHover, (int)upgrade);
	}

	public void EmitUpdateUpgradesAvailability()
	{
		bool upgradesUnlocked = _sceneResources.StructureCount[StructureType.ResearchLab] > 0;

		_sceneResources.UpgradesAvailable = upgradesUnlocked;

		EmitSignal(SignalName.UpdateUpgradesAvailability);
	}

	public void EmitUpdateVehicleAvailability()
	{
		bool vehiclesUnlocked = _sceneResources.StructureCount[StructureType.Garage] > 0;

		_sceneResources.VehicleAvailability[VehicleType.TankGen1] = vehiclesUnlocked;
		_sceneResources.VehicleAvailability[VehicleType.TankGen2] = vehiclesUnlocked;
		_sceneResources.VehicleAvailability[VehicleType.Artillery] = vehiclesUnlocked;

		EmitSignal(SignalName.UpdateVehicleAvailability);
	}

	public void EmitUpdateInfantryAvailability()
	{
		bool infantryUnlocked = _sceneResources.StructureCount[StructureType.Barracks] > 0;

		_sceneResources.InfantryAvailability[InfantryType.Infantry] = infantryUnlocked;

		EmitSignal(SignalName.UpdateInfantryAvailability);
	}

	public void EmitBuildVehicle(Vehicle vehicle)
	{
		EmitSignal(SignalName.BuildVehicle, vehicle);
	}

	public void EmitBuildInfantry(Infantry infantry)
	{
		EmitSignal(SignalName.BuildInfantry, infantry);
	}
}
