using Godot;
using MyEnums;

[Tool]
public partial class GlobalResources : Node3D
{
    public static GlobalResources Instance { get; set; }
    [Export] public Vector2 MapSize { get; set; }
    [Export] public Season Season { get; set; }
    [Export] public TimeOfDay TimeOfDay { get; set; }
    [Export] public Weather Weather { get; set; }
    public bool IsPlacingStructure { get; set; }
    public bool IsHoveringUI { get; set; }

    public override void _EnterTree()
    {
        base._EnterTree();  // **must** call this first
        GD.Print("GlobalResources _EnterTree called");
        Instance = this;

        if (MapSize == Vector2.Zero) Utils.PrintErr("MapSize is not set");
        if (Weather == Weather.None) Utils.PrintErr("Weather is set to None.");
        if (Season == Season.None) Utils.PrintErr("Season is set to None.");
        if (TimeOfDay == TimeOfDay.None) Utils.PrintErr("TimeOfDay is set to None.");
    }
}
