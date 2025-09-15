public partial class TankGen2 : Vehicle
{
	public override void _Ready()
	{
		base._Ready();

		this.UnitClass = MyEnums.UnitClass.HeavyVehicle;
	}
}
