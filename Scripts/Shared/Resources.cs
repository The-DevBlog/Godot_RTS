using Godot;

public partial class Resources : Node
{
    public static Resources Instance { get; set; }
    public bool IsPlacingStructure { get; set; }
    public Resources() => Instance = this;
}
