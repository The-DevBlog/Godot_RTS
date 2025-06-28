using Godot;
using MyEnums;

public partial class VehicleBtn : Button
{
    [Export] public VehicleType Vehicle { get; set; }
    private Signals _signals;
    private MyModels _models;
    private SceneResources _sceneResources;
    private TextureRect _lockTexture;
    private Color _normalModulate = new Color("#c8c8c8");
    private Color _hoverModulate = new Color("#ffffff");
    private Color _disabledModulate = new Color("#737373");
    public override void _Ready()
    {
        _models = AssetServer.Instance.Models;
        _lockTexture = GetNode<TextureRect>("LockTexture");
        _sceneResources = SceneResources.Instance;
        _signals = Signals.Instance;

        if (Vehicle == VehicleType.None) Utils.PrintErr("VehicleType is to set None");
        if (_lockTexture == null) Utils.PrintErr("LockTexture not found for unit: " + Vehicle.ToString());

        _signals.UpdateVehicleAvailability += EnableDisableBtns;
        MouseEntered += OnMouseEnter;
        MouseExited += OnMouseExit;
        Pressed += OnUnitSelect;
        SelfModulate = Disabled ? _disabledModulate : _normalModulate;
    }

    private void OnUnitSelect()
    {
        var unit = _models.Vehicles[Vehicle];
        Vehicle vehicleInstance = unit.Instantiate<Vehicle>();

        bool enoughFunds = _sceneResources.Funds >= vehicleInstance.Cost;
        if (!enoughFunds)
        {
            GD.Print("Not enough funds!");
            return;
        }

        _signals.EmitBuildVehicle(vehicleInstance);
        _signals.EmitUpdateFunds(-vehicleInstance.Cost);
    }

    private void OnMouseEnter()
    {
        if (!_models.Vehicles.ContainsKey(Vehicle))
        {
            Utils.PrintErr("MyModels.cs -> Units dictionary does not contains key for UnitType: " + Vehicle);
            return;
        }

        if (!Disabled)
            SelfModulate = _hoverModulate;

        var packed = _models.Vehicles[Vehicle];
        var unit = packed.Instantiate<Unit>();

        _signals.EmitBuildOptionsBtnBtnHover(null, unit);
    }

    private void OnMouseExit()
    {
        SelfModulate = Disabled ? _disabledModulate : _normalModulate;
        _signals.EmitBuildOptionsBtnBtnHover(null, null);
    }

    private void EnableDisableBtns()
    {
        Disabled = !_sceneResources.VehicleAvailability[Vehicle];
        SelfModulate = Disabled ? _disabledModulate : _normalModulate;
        _lockTexture.Visible = Disabled;
    }
}
