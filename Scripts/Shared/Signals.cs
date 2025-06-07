using Godot;

public partial class Signals : Node
{
    public static Signals Instance { get; private set; }
    [Signal] public delegate void UpdateNavigationMapEventHandler(NavigationRegion3D region);
    [Signal] public delegate void DeselectAllUnitsEventHandler();
    [Signal] public delegate void UpdateEnergyEventHandler(int energy);
    private Resources _resources;

    public override void _Ready()
    {
        Instance = this;
        _resources = Resources.Instance;
    }

    public void EmitUpdateNavigationMap(NavigationRegion3D region) => EmitSignal(SignalName.UpdateNavigationMap, region);

    public void EmitUpdateEnergy(int energy)
    {
        GD.Print("Update Energy: " + energy);
        _resources.Energy += energy;
        EmitSignal(SignalName.UpdateEnergy, energy);
    }
}
