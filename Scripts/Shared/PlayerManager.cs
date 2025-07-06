using System.Collections.Generic;
using Godot;

public partial class PlayerManager : Node
{
	public static PlayerManager Instance { get; private set; }

	// All connected players, keyed by peer ID
	private readonly Dictionary<long, Player> _players = new();

	public Player LocalPlayer { get; private set; }
	public Player Authority { get; set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public void AddLocalPlayer()
	{
		var meId = Multiplayer.GetUniqueId(); // usually 1 if no network yet
		var me = InstantiatePlayer(meId);
		AddPlayer(meId, me);
	}

	public void AddPlayer(long peerId, Player player)
	{
		GD.Print($"Adding player with peer ID: {peerId}");

		_players[peerId] = player;
		if (peerId == Multiplayer.GetUniqueId())
		{
			LocalPlayer = player;
			Authority = player;
		}
	}

	public Player GetPlayer(int peerId)
	{
		_players.TryGetValue(peerId, out var p);
		return p;
	}

	public void RemovePlayer(int peerId)
	{
		if (_players.Remove(peerId, out var p))
			p.QueueFree();

		if (LocalPlayer?.GetMultiplayerAuthority() == peerId)
			LocalPlayer = null;

		if (Authority?.GetMultiplayerAuthority() == peerId)
			Authority = null;
	}

	private Player InstantiatePlayer(int peerId)
	{
		var player = /* load or reference your Player PackedScene */
					  ResourceLoader.Load<PackedScene>("res://Scenes/player.tscn")
									.Instantiate<Player>();

		player.Name = peerId.ToString();
		player.SetMultiplayerAuthority(peerId);
		AddChild(player);
		return player;
	}
}
