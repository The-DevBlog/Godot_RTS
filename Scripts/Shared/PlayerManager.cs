using System.Collections.Generic;
using Godot;
using MyEnums;

public partial class PlayerManager : Node
{
	public static PlayerManager Instance { get; private set; }
	public Player LocalPlayer { get; set; }
	public Player Authority { get; set; }
	private Dictionary<int, Player> _stagedPlayers = new();
	public override void _EnterTree()
	{
		Instance = this;
	}

	public void StagePlayer(Player player)
	{
		_stagedPlayers[player.Id] = player;
		GD.Print($"Player {player.Id} staged");
	}

	public void UnstagePlayer(int playerId)
	{
		if (_stagedPlayers.ContainsKey(playerId))
			_stagedPlayers.Remove(playerId);

		GD.Print($"Player {playerId} unstaged");
	}

	// public void SpawnPlayers(Node3D playerSpawnNode, MultiplayerSpawner playerSpawner)
	// {
	// 	// if (!Multiplayer.IsServer())
	// 	// 	return;

	// 	GD.Print("In SpawnPlayers()");

	// 	// var sceneRoot = GetTree().CurrentScene;
	// 	// var playerSpawner = sceneRoot.GetNodeOrNull<MultiplayerSpawner>("PlayerSpawner");
	// 	// if (playerSpawner == null)
	// 	// {
	// 	// 	Utils.PrintErr("PlayerSpawner not found!");
	// 	// 	return;
	// 	// }

	// 	foreach (var kv in _stagedPlayers)
	// 	{
	// 		Player playerInfo = kv.Value;

	// 		PackedScene playerScene = AssetServer.Instance.Scenes.Scenes[SceneType.Player];
	// 		Player player = playerScene.Instantiate<Player>();

	// 		player.Name = "Player_" + playerInfo.Id;
	// 		player.Id = playerInfo.Id;
	// 		player.Color = playerInfo.Color;
	// 		player.Funds = playerInfo.Funds;
	// 		player.Team = playerInfo.Team;

	// 		GD.Print("HELLO");
	// 		if (Multiplayer.IsServer())
	// 			SpawnPlayer(playerSpawnNode, player);
	// 		// ServerSpawnStructure(player.Id, finalXform, (int)placeholder.StructureType);
	// 		else
	// 			RpcId(1, nameof(SpawnPlayer), playerSpawnNode, player);
	// 		// RpcId(1, nameof(ServerSpawnStructure), player.Id, finalXform, (int)placeholder.StructureType);

	// 		playerSpawnNode.AddChild(player);

	// 		// var args = new Godot.Collections.Array {
	// 		// 	(long)kv.Value.Id,
	// 		// 	kv.Value.Color,
	// 		// 	kv.Value.Funds,
	// 		// 	kv.Value.Team
	// 		// };
	// 		// playerSpawner.Spawn(args);   // <— this drives the replication

	// 		GD.Print("Spawned player: " + playerInfo.Id);
	// 	}

	// 	_stagedPlayers.Clear();
	// }

	public void SpawnPlayers()
	{
		if (!Multiplayer.IsServer())
			return;  // only the host runs this

		GD.Print("Server spawning all staged players…");

		foreach (var kv in _stagedPlayers)
		{
			var info = kv.Value;

			// —— 1) Spawn locally —— 
			RpcSpawnPlayer(info.Id, info.Color, info.Funds, info.Team);

			// —— 2) Tell everyone else —— 
			//    RpcId(0, ...) goes to ALL other peers
			RpcId(0, nameof(RpcSpawnPlayer),
				  info.Id,
				  info.Color,
				  info.Funds,
				  info.Team);

			GD.Print($"  -> Told everyone to spawn Player_{info.Id}");
		}

		_stagedPlayers.Clear();
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcSpawnPlayer(int id, Color color, int funds, int team)
	{
		// 1) Instantiate the scene on *this* peer
		var scene = AssetServer.Instance.Scenes.Scenes[SceneType.Player];
		var player = scene.Instantiate<Player>();

		// 2) Initialize it
		player.Name = $"Player_{id}";
		player.Id = id;
		player.Color = color;
		player.Funds = funds;
		player.Team = team;

		// 3) Parent it under your Players container
		var playersNode = GetTree()
			.CurrentScene
			.GetNode<Node3D>("Players");

		if (playersNode == null)
			GD.PrintErr("Players node not found in the scene tree.");

		playersNode.AddChild(player);
	}
}
