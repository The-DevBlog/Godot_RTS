using Godot;
using System;

public partial class StressTest : Node3D
{
	[Export]
	public PackedScene UnitScene { get; set; }

	[Export]
	public int CountX { get; set; } = 10;
	[Export]
	public int CountZ { get; set; } = 10;

	[Export]
	public int Spacing { get; set; } = 2;

	public override void _Ready()
	{
		if (UnitScene == null)
		{
			GD.PrintErr("StressTest: No SceneToSpawn assigned!");
			return;
		}

		// Center the grid around this node’s origin
		Vector3 offset = new Vector3(
			-(CountX - 1) * Spacing * 0.5f,
			0,
			-(CountZ - 1) * Spacing * 0.5f
		);

		for (int x = 0; x < CountX; x++)
		{
			for (int z = 0; z < CountZ; z++)
			{
				// Instance and add
				Node3D instance = UnitScene.Instantiate<Node3D>();
				AddChild(instance);

				// Compute position in the grid
				Vector3 pos = new Vector3(x * Spacing, 0, z * Spacing) + offset;

				instance.Transform = new Transform3D(instance.Transform.Basis, pos);
			}
		}
	}
}
