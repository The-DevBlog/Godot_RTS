using Godot;

[Tool]
public partial class MultiplayerSpawner : Godot.MultiplayerSpawner
{
	public void DoSpawnStructure(PackedScene scene, Variant vData)
	{
		var arr = (Godot.Collections.Array)vData;
		var regionPath = (NodePath)arr[0];
		var pos = (Vector3)arr[1];
		var rotQuat = (Quaternion)arr[2];

		var navRegion = GetNode<NavigationRegion3D>(regionPath);

		// 1) instantiate
		var inst = scene.Instantiate<StructureBase>();

		// 2) set its full transform before parenting
		inst.GlobalTransform = new Transform3D(new Basis(rotQuat), pos);

		// 3) parent under the correct NavigationRegion3D
		navRegion.AddChild(inst);

		// 4) tag, register, navmesh rebuild…
	}
}
