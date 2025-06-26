using System.Collections.Generic;
using Godot;
using MyEnums;

public partial class SceneResources : Node3D
{
	public static SceneResources Instance { get; set; }
	[Export] public Vector2 MapSize { get; set; }
	[Export] public int Funds { get; set; }
	[Export] public Color TeamColor;
	[Export] public bool RainyWeather { get; set; }
	public int Energy { get; set; }
	public int EnergyConsumed { get; set; }
	public int MaxStructureCount; // max structure count for garage and barracks
	public Dictionary<StructureType, int> StructureCount { get; } = new();
	public Dictionary<InfantryType, bool> InfantryAvailability { get; private set; } = new();
	public Dictionary<VehicleType, bool> VehicleAvailability { get; private set; } = new();
	public HashSet<Garage> GaragesMap { get; } = new();
	public HashSet<Barracks> BarracksMap { get; } = new();
	public int ActiveGarageId { get; set; } = 0;
	public int ActiveBarracksId { get; set; } = 0;
	public bool UpgradesAvailable { get; set; }

	public SceneResources()
	{
		MaxStructureCount = 8;

		foreach (StructureType s in StructureType.GetValues(typeof(StructureType)))
			StructureCount[s] = 0;

		foreach (var unit in InfantryType.GetValues<InfantryType>())
			InfantryAvailability[unit] = false;

		foreach (var vehicle in VehicleType.GetValues<VehicleType>())
			VehicleAvailability[vehicle] = false;
	}

	public override void _EnterTree()
	{
		base._EnterTree();  // **must** call this first
		Instance = this;

		if (Funds == 0)
			Utils.PrintErr("No Funds Assigned");
	}

	public void AddStructure(StructureType structure)
	{
		StructureCount[structure]++;
	}

	public void RemoveStructure(StructureType structure)
	{
		if (StructureCount[structure] <= 0)
		{
			Utils.PrintErr($"Cannot reduce structure count: {structure} as count is already zero.");
			return;
		}

		StructureCount[structure]--;
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
}
