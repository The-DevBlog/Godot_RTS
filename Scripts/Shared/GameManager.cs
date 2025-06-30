using Godot;
using MyEnums;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
	}

	// This RPC will run on all peers when you call Rpc(SpawnStructureRpc,…)
	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void SpawnStructureRpc(int structureType, Vector3 pos, Basis rot)
	{
		PackedScene scene = AssetServer.Instance.Models.Structures[(StructureType)structureType];
		StructureBase structure = scene.Instantiate<StructureBase>();
		structure.GlobalTransform = new Transform3D(rot, pos);
		AddChild(structure);
	}

	/// <summary>
	/// Call this when you want to spawn a structure across the network.
	/// </summary>
	public void RequestSpawnStructure(StructureType type, Transform3D xform)
	{
		int t = (int)type;
		// If you’re the host/server, broadcast to everyone
		if (NetworkManager.Instance.IsServer)
			Rpc(nameof(SpawnStructureRpc), t, xform.Origin, xform.Basis);
		else
			// If you’re a client, send only to the server (peer ID 1)
			RpcId(1, nameof(SpawnStructureRpc), t, xform.Origin, xform.Basis);
	}
}
