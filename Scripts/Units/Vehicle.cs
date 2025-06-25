using Godot;
using MyEnums;

public partial class Vehicle : Unit
{
    [Export] public VehicleType VehicleType { get; set; }
    public override bool Unlocked => SceneResources.Instance.VehicleAvailability[VehicleType];
    private Signals _signals;

    public override void _Ready()
    {
        base._Ready();

        _signals = Signals.Instance;

        _signals.UpdateVehicleAvailability += Blah;

        if (VehicleType == VehicleType.None) Utils.PrintErr("VehicleType is not set for vehicle");
    }

    private void Blah()
    {
        GD.Print("BLAH");
    }
}
