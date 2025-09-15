public partial class AntiInfantry : Vehicle
{
	public override void _Ready()
	{
		base._Ready();

		this.UnitClass = MyEnums.UnitClass.LightVehicle;
	}
}
