using Godot;

public partial class Signals : Node
{
    public static Signals Instance { get; private set; }

    [Signal]
    public delegate void UpdateNavigationMapEventHandler(NavigationRegion3D region);
    [Signal]
    public delegate void DeselectAllUnitsEventHandler();

    public override void _Ready()
    {
        Instance = this;
    }

    public void EmitUpdateNavigationMap(NavigationRegion3D region) => EmitSignal(SignalName.UpdateNavigationMap, region);
}
