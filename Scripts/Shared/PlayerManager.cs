using System.Collections.Generic;
using Godot;

public partial class PlayerManager : Node
{
	public static PlayerManager Instance { get; private set; }

	// All players by ID
	private readonly Dictionary<int, Player> _players = new();

	// The one your local UI/inputs drive
	public Player LocalPlayer { get; private set; }

	public override void _EnterTree()
	{
		base._EnterTree();
		Instance = this;
	}

	// Called from each Player._EnterTree()
	public void RegisterPlayer(Player p)
	{
		_players[p.PlayerId] = p;
		if (p.IsHuman && LocalPlayer == null)
			LocalPlayer = p;
	}

	public Player GetPlayer(int id)
		=> _players.TryGetValue(id, out var p) ? p : null;

	public IEnumerable<Player> GetAllPlayers() => _players.Values;
}
