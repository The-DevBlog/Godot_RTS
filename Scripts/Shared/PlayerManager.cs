using System.Collections.Generic;
using Godot;
using MyEnums;

public partial class PlayerManager : Node
{
	public static PlayerManager Instance { get; private set; }
	public Player LocalPlayer { get; set; }
	public Player Authority { get; set; }
	private Dictionary<int, Player> _stagedPlayers = new();
	// private MyScenes _scenes;

	public override void _EnterTree()
	{
		Instance = this;
		// _scenes = AssetServer.Instance.Scenes;
		// Utils.NullCheck(_scenes);
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

	public void SpawnPlayers(Node3D playerSpawnNode, MultiplayerSpawner playerSpawner)
	{
		if (!Multiplayer.IsServer())
			return;

		GD.Print("In SpawnPlayers()");

		// var sceneRoot = GetTree().CurrentScene;
		// var playerSpawner = sceneRoot.GetNodeOrNull<MultiplayerSpawner>("PlayerSpawner");
		// if (playerSpawner == null)
		// {
		// 	Utils.PrintErr("PlayerSpawner not found!");
		// 	return;
		// }

		foreach (var kv in _stagedPlayers)
		{
			Player playerInfo = kv.Value;

			PackedScene playerScene = AssetServer.Instance.Scenes.Scenes[SceneType.Player];
			Player player = playerScene.Instantiate<Player>();

			player.Name = "Player_" + playerInfo.Id;
			player.Id = playerInfo.Id;
			player.Color = playerInfo.Color;
			player.Funds = playerInfo.Funds;
			player.Team = playerInfo.Team;

			// playerSpawnNode.AddChild(player);

			var args = new Godot.Collections.Array {
				(long)kv.Value.Id,
				kv.Value.Color,
				kv.Value.Funds,
				kv.Value.Team
			};
			playerSpawner.Spawn(args);   // <— this drives the replication

			GD.Print("Spawned player: " + playerInfo.Id);
		}

		_stagedPlayers.Clear();
	}
}
