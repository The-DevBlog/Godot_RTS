using Godot;
public partial class BFT : Vehicle
{
	// [Export] public WeaponSystem SecondaryWeaponSystem { get; set; }

	public override void _Ready()
	{
		base._Ready();

		// Utils.NullExportCheck(SecondaryWeaponSystem);
	}
}
