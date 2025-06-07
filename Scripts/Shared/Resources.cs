using Godot;

public partial class Resources : Node
{
    public static Resources Instance { get; set; }
    public bool IsPlacingStructure { get; set; }
    public bool IsHoveringUI { get; set; }
    public int Energy { get; set; }
    public int EnergyConsumed { get; set; }
    public Resources() => Instance = this;
}
