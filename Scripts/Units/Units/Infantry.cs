using Godot;

public partial class Infantry : Unit
{
	[Export] public MyEnums.InfantryType InfantryType { get; set; }
	private bool _movingFlag = false;

	public override void _Ready()
	{
		base._Ready();

		this.UnitClass = MyEnums.UnitClass.Infantry;

		if (InfantryType == MyEnums.InfantryType.None) Utils.PrintErr("InfantryType is not set for infantry");
	}

	public override void ShootAnimation()
	{
		GD.Print("Shoot Animation");
		AnimationPlayer.Play("Shoot");
	}

	private protected override void MoveAnimation()
	{
		GD.Print("Move Animation");
		AnimationPlayer.Play("Move");
	}

	private protected override void IdleAnimation()
	{
		GD.Print("Idle Animation");
		AnimationPlayer.Play("Idle");
	}
}
