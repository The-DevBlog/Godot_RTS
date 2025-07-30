using System.Collections.Generic;
using Godot;

public partial class PlayerManager : Node
{
	public static PlayerManager Instance { get; private set; }

	private readonly Dictionary<long, Player> _players = new();
	private Dictionary<int, Player> _playersToAdd = new();

	public Player LocalPlayer { get; set; }
	public Player Authority { get; set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public void StagePlayer(Player player)
	{
		_playersToAdd[player.Id] = player;
		GD.Print($"Player {player.Id} staged");
	}

	public void UnstagePlayer(int playerId)
	{
		if (_playersToAdd.ContainsKey(playerId))
			_playersToAdd.Remove(playerId);

		GD.Print($"Player {playerId} unstaged");
	}
}
