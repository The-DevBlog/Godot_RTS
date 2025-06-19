using System.Collections.Generic;
using Godot;
using MyEnums;

public partial class GlobalResources : Node
{
	public static GlobalResources Instance { get; set; }
	public Dictionary<StructureType, int> StructureCount { get; }
	public bool IsPlacingStructure { get; set; }
	public bool IsHoveringUI { get; set; }
	public int Energy { get; set; }
	public int EnergyConsumed { get; set; }
	public int Funds { get; set; }
	public int MaxStructureCount;
	public Color TeamColor;
	public GlobalResources()
	{
		Instance = this;
		Funds = 1000000;
		MaxStructureCount = 8;
		TeamColor = Colors.OrangeRed;

		StructureCount = new Dictionary<StructureType, int>();
		foreach (StructureType s in StructureType.GetValues(typeof(StructureType)))
			StructureCount[s] = 0;
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
