using Godot;

public partial class GlobalResources : Node
{
    public static GlobalResources Instance { get; set; }
    public bool IsPlacingStructure { get; set; }
    public bool IsHoveringUI { get; set; }
    public GlobalResources()
    {
        Instance = this;
    }
}
