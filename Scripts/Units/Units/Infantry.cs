using Godot;
using MyEnums;

public partial class Infantry : Unit
{
	[Export] public InfantryType InfantryType { get; set; }
	private bool _movingFlag = false;

	public override void _Ready()
	{
		base._Ready();

		if (InfantryType == InfantryType.None) Utils.PrintErr("InfantryType is not set for infantry");
	}

	private protected override void MoveAnimation()
	{
		GD.Print("Playing Run Animation from Infantry.cs");
		AnimationPlayer.Play("Move");
	}

	private protected override void IdleAnimation()
	{
		GD.Print("Playing Idle Animation from Infantry.cs");
		AnimationPlayer.Play("Idle");
	}
}
