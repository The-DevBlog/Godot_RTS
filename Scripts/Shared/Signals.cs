using System;
using Godot;
using MyEnums;

public partial class Signals : Node
{
    public static Signals Instance { get; private set; }
    [Signal] public delegate void UpdateNavigationMapEventHandler(NavigationRegion3D region);
    [Signal] public delegate void DeselectAllUnitsEventHandler();
    [Signal] public delegate void UpdateEnergyEventHandler();
    [Signal] public delegate void UpdateFundsEventHandler();
    [Signal] public delegate void OnBuildOptionsBtnHoverEventHandler(StructureBase structure, UnitBase unit);
    [Signal] public delegate void AddStructureEventHandler(int structureId);
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

        if (energy > 0)
            _resources.Energy += energy;
        else if (energy < 0)
            _resources.EnergyConsumed += Math.Abs(energy);

        EmitSignal(SignalName.UpdateEnergy);
    }

    public void EmitUpdateFunds(int funds)
    {
        GD.Print("Update Funds: " + funds);

        _resources.Funds += funds;
        EmitSignal(SignalName.UpdateFunds);
    }

    public void EmitAddStructure(StructureType structure)
    {
        GD.Print("Add Structure: " + structure);

        _resources.AddStructure(structure);
        EmitSignal(SignalName.AddStructure, (int)structure);
    }

    public void EmitBuildOptionsBtnBtnHover(StructureBase structure, UnitBase unit)
    {
        EmitSignal(SignalName.OnBuildOptionsBtnHover, structure, unit);
    }

}
