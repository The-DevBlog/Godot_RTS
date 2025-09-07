using Godot;
using MyEnums;

public partial class WeaponSystem : Node
{
	[Export] public int Dmg { get; set; }
	[Export] public int Range { get; set; }
	[Export] public float FireRate { get; set; }
	[Export] public float ProjectileSpeed { get; set; }
	[Export] public float BulletSpread { get; set; }
	[Export] public WeaponType WeaponType { get; set; }

	public override void _Ready()
	{
		if (Dmg == 0) Utils.PrintErr("No DPS Assigned to unit");
		if (Range == 0) Utils.PrintErr("No Range Assigned to unit");
		if (FireRate == 0) Utils.PrintErr("No FireRate Assigned to unit");
		if (ProjectileSpeed == 0) Utils.PrintErr("No ProjectileSpeed Assigned to unit");
		if (BulletSpread == 0) Utils.PrintErr("No BulletSpread Assigned to unit");
		if (WeaponType == WeaponType.None) Utils.PrintErr("No WeaponType Assigned to unit");
	}
}
