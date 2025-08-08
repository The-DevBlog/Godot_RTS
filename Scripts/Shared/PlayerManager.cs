// using System.Collections.Generic;
// using Godot;

// public partial class PlayerManager : Node
// {
// 	public static PlayerManager Instance { get; private set; }

// 	// All connected players, keyed by peer ID
// 	private readonly Dictionary<long, Player> _players = new();

// 	public Player LocalPlayer { get; private set; }
// 	public Player Authority { get; set; }

// 	public override void _Ready()
// 	{
// 		Instance = this;

// 		// Immediately create/register *your* Player
// 		// var meId = Multiplayer.GetUniqueId(); // usually 1 if no network yet
// 		// var me = InstantiatePlayer(meId);
// 		// AddPlayer(meId, me);
// 	}

// 	// private Player InstantiatePlayer(int peerId)
// 	// {
// 	// 	var player = /* load or reference your Player PackedScene */
// 	// 				  ResourceLoader.Load<PackedScene>("res://Scenes/Player.tscn")
// 	// 								.Instantiate<Player>();

// 	// 	player.Name = peerId.ToString();
// 	// 	player.SetMultiplayerAuthority(peerId);
// 	// 	AddChild(player);
// 	// 	return player;
// 	// }
// }

// PlayerManager.cs (autoload, set up in Project Settings → AutoLoad)
using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerManager : Node
{
	public static PlayerManager Instance { get; private set; }

	// All players (human + bots)
	public List<Player> Players { get; } = new();

	// Convenience pointer to your one human player
	public Player HumanPlayer { get; private set; }

	[Signal] public delegate void HumanPlayerReadyEventHandler(Player player);

	public void RegisterPlayer(Player p)
	{
		Players.Add(p);
		if (p.IsHuman && HumanPlayer == null)
		{
			HumanPlayer = p;
			EmitSignal(SignalName.HumanPlayerReady, p);
		}

		GD.Print($"Registered player: {p.Name} (ID: {p.Id})");
	}

	// public override void _Ready()
	// {
	// GD.Print("PlayerManager ready");
	// Instance = this;

	// var root = GetTree().Root;
	// if (root == null)
	// {
	// 	GD.PrintErr("Root node not found in the scene tree.");
	// 	return;
	// }

	// Node playerNode = GetTree().Root.GetNodeOrNull("Players");
	// if (playerNode == null)
	// {
	// 	GD.PrintErr("Player node not found in the scene tree. Ensure it is added to the root node.");
	// 	return;
	// }

	// // (Optional) find any Players already in the scene:
	// foreach (Player p in GetTree().GetNodesInGroup("Players"))
	// 	RegisterPlayer(p);
	// }

	public override void _Ready()
	{
		Instance = this;
	}

	// public void RegisterPlayer(Player p)
	// {
	// 	GD.Print($"Registering player: {p.Name} (ID: {p.Id})");
	// 	Players.Add(p);
	// 	if (p.IsHuman)
	// 		HumanPlayer = p;

	// 	GD.Print($"Registered player: {p.Name} (ID: {p.Id})");
	// }

	public Player GetPlayerById(int id) =>
		Players.FirstOrDefault(p => p.Id == id);
}
