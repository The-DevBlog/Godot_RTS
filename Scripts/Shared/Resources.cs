using System.Collections.Generic;
using Godot;
using MyEnums;

public partial class Resources : Node
{
    public static Resources Instance { get; set; }
    public Dictionary<Structure, int> StructureCount { get; }
    public bool IsPlacingStructure { get; set; }
    public bool IsHoveringUI { get; set; }
    public int Energy { get; set; }
    public int EnergyConsumed { get; set; }
    public int Funds { get; set; }
    public Resources()
    {
        Instance = this;
        Funds = 10000;

        StructureCount = new Dictionary<Structure, int>();
        foreach (Structure s in Structure.GetValues(typeof(Structure)))
            StructureCount[s] = 0;
    }

    public void AddStructure(Structure structure)
    {
        StructureCount[structure]++;
    }

    public void RemoveStructure(Structure structure)
    {
        if (StructureCount[structure] <= 0)
        {
            Utils.PrintErr($"Cannot reduce structure count: {structure} as count is already zero.");
            return;
        }

        StructureCount[structure]--;
    }
}
