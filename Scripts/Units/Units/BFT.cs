using Godot;
public partial class BFT : Vehicle
{
    [ExportCategory("Weapons")]
    [Export] public WeaponSystem SecondaryWeaponSystem { get; set; }

    public override void _Ready()
    {
        base._Ready();

        this.UnitClass = MyEnums.UnitClass.HeavyVehicle;

        Utils.NullExportCheck(SecondaryWeaponSystem);
    }
}
