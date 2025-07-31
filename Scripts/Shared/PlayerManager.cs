using System.Collections.Generic;
using Godot;

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

	public void SpawnPlayers()
	{
		if (!Multiplayer.IsServer())
			return;

		GD.Print("In SpawnPlayers()");

		var sceneRoot = GetTree().CurrentScene;
		var playerSpawner = sceneRoot.GetNode<MultiplayerSpawner>("PlayerSpawner");
		if (playerSpawner == null)
		{
			Utils.PrintErr("PlayerSpawner not found!");
			return;
		}

		foreach (var kv in _stagedPlayers)
		{
			Player player = kv.Value;

			// pack the initial data into a VariantArray
			var args = new Godot.Collections.Array { (long)player.Id, player.Color, player.Funds, player.Team };

			// spawner.Spawn(args);
			playerSpawner.Spawn(args);
			GD.Print("Spawned player: " + player.Id);
		}

		_stagedPlayers.Clear();
	}
}
