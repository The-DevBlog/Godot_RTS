using System;
using System.Collections.Generic;
using Godot;
using MyEnums;

public partial class Player : Node3D
{
	[Export] public int Id { get; set; }
	[Export] public bool IsHuman { get; set; }    // drive from UI or AI
	[Export] public Color Color { get; set; }
	[Export] public int Funds { get; private set; }

	[Signal] public delegate void OnUpdateEnergyEventHandler(int amount);
	[Signal] public delegate void OnUpdateFundsEventHandler(int amount);
	[Signal] public delegate void BuildVehicleEventHandler(Vehicle vehicle);
	[Signal] public delegate void OnAddBarracksEventHandler(int barracksCount);
	[Signal] public delegate void OnAddGarageEventHandler(int garageCount);
	[Signal] public delegate void UpdateInfantryAvailabilityEventHandler();
	[Signal] public delegate void UpdateVehicleAvailabilityEventHandler();
	[Signal] public delegate void UpdateUpgradesAvailabilityEventHandler();
	[Signal] public delegate void BuildInfantryEventHandler(Infantry infantry);
	[Signal] public delegate void DeselectAllUnitsEventHandler();
	[Signal] public delegate void SelectUnitsEventHandler(Unit[] units);

	public int MaxStructureCount; // max structure count for garage and barracks
	public Dictionary<StructureType, int> StructureCount { get; } = new();
	public int Energy { get; private set; }
	public int EnergyConsumed { get; private set; }
	public List<Unit> Units { get; } = new();
	public List<StructureBase> Structures { get; } = new();
	public Dictionary<InfantryType, bool> InfantryAvailability { get; private set; } = new();
	public Dictionary<VehicleType, bool> VehicleAvailability { get; private set; } = new();
	public HashSet<Garage> GaragesMap { get; } = new();
	public HashSet<Barracks> BarracksMap { get; } = new();
	public int ActiveGarageId { get; set; } = 0;
	public int ActiveBarracksId { get; set; } = 0;
	public bool UpgradesAvailable { get; set; }

	// public override void _EnterTree()
	// {
	// 	base._EnterTree();

	// 	// TODO: Remove these hard coded values
	// 	// Color = new Color("0083fa");
	// 	// Funds = 50000;

	// 	// PlayerManager.Instance.RegisterPlayer(this);

	// 	// Utils.NullExportCheck(Color);

	// 	// if (Color == default)
	// 	// 	Color = new Color(Colors.Red);

	// 	if (Funds == 0) Utils.PrintErr("Funds not set for player " + Id);

	// 	MaxStructureCount = 8;

	// 	foreach (StructureType s in StructureType.GetValues(typeof(StructureType)))
	// 		StructureCount[s] = 0;

	// 	foreach (var unit in InfantryType.GetValues<InfantryType>())
	// 		InfantryAvailability[unit] = false;

	// 	foreach (var vehicle in VehicleType.GetValues<VehicleType>())
	// 		VehicleAvailability[vehicle] = false;

	// }

	public override void _Ready()
	{
		PlayerManager.Instance.RegisterPlayer(this);
		// TODO: Remove these hard coded values
		// Color = new Color("0083fa");
		// Funds = 50000;

		// PlayerManager.Instance.RegisterPlayer(this);

		Utils.NullExportCheck(Color);

		// if (Color == default)
		// 	Color = new Color(Colors.Red);

		if (Funds == 0) Utils.PrintErr("Funds not set for player " + Id);

		MaxStructureCount = 8;

		foreach (StructureType s in StructureType.GetValues(typeof(StructureType)))
			StructureCount[s] = 0;

		foreach (var unit in InfantryType.GetValues<InfantryType>())
			InfantryAvailability[unit] = false;

		foreach (var vehicle in VehicleType.GetValues<VehicleType>())
			VehicleAvailability[vehicle] = false;


		GD.Print("Player funds: " + Funds);
		GD.Print("Player color: " + Color);

	}

	public void UpdateFunds(int amount)
	{
		GD.Print("Updating funds for player " + Id + ": " + amount);

		Funds += amount;

		EmitSignal(SignalName.OnUpdateFunds, amount);
	}
	public void UpdateEnergy(int amount)
	{
		GD.Print("Updating energy for player " + Id + ": " + amount);

		if (amount > 0)
			Energy += amount;
		else if (amount < 0)
			EnergyConsumed += Math.Abs(amount);

		EmitSignal(SignalName.OnUpdateEnergy, amount);
		// TODO: Fix
		// if (EnergyConsumed > Energy)
		// 	EmitSignal(SignalName.UpdateEnergyColor);
	}
	public void RegisterUnit(Unit u) => Units.Add(u);
	public void UnregisterUnit(Unit u) => Units.Remove(u);
	public void RegisterStructure(StructureBase s) => Structures.Add(s);
	public void UnregisterStructure(StructureBase s) => Structures.Remove(s);

	public void AddStructure(StructureBase structure)
	{
		StructureCount[structure.StructureType]++;

		if (structure is Garage garage)
		{
			GaragesMap.Add(garage);

			foreach (VehicleType vehicle in System.Enum.GetValues<VehicleType>())
				VehicleAvailability[vehicle] = true;

			EmitSignal(SignalName.UpdateVehicleAvailability);
			EmitSignal(SignalName.OnAddGarage, GaragesMap.Count);
		}
		else if (structure is Barracks barracks)
		{
			BarracksMap.Add(barracks);

			foreach (InfantryType infantry in System.Enum.GetValues<InfantryType>())
				InfantryAvailability[infantry] = true;

			EmitSignal(SignalName.UpdateInfantryAvailability);
			EmitSignal(SignalName.OnAddBarracks, BarracksMap.Count);
		}
		else if (structure.StructureType == StructureType.ResearchLab)
		{
			UpgradesAvailable = true;
			EmitSignal(SignalName.UpdateUpgradesAvailability);
		}

		RegisterStructure(structure);
		UpdateFunds(-structure.Cost);
		UpdateEnergy(structure.Energy);
	}

	public bool MaxStructureCountReached(StructureType structure)
	{
		bool reached;
		if (structure != StructureType.Garage && structure != StructureType.Barracks)
			return false;
		else
			reached = StructureCount[structure] >= MaxStructureCount;

		if (reached)
			GD.Print("Max structure count reached for: " + structure);

		return reached;
	}

	public void EmitBuildInfantry(Infantry infantry)
	{
		EmitSignal(SignalName.BuildInfantry, infantry);
	}

	public void EmitBuildVehicle(Vehicle vehicle)
	{
		EmitSignal(SignalName.BuildVehicle, vehicle);
	}

	public void EmitSelectUnits(Unit[] units)
	{
		GD.Print("Select Units: " + units.Length);
		EmitSignal(SignalName.SelectUnits, units);
	}
}
