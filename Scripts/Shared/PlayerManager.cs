using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerManager : Node
{
	public static PlayerManager Instance { get; private set; }
	public List<Player> Players { get; } = new(); // Human and bots
	public Player HumanPlayer { get; private set; }
	public event Action<Player> HumanPlayerReady;
	public override void _Ready()
	{
		Instance = this;
	}

	/// <summary>
	/// Called by Player when it’s ready.
	/// </summary>
	public void RegisterPlayer(Player p)
	{
		Players.Add(p);
		if (p.IsHuman)
		{
			HumanPlayer = p;
			HumanPlayerReady?.Invoke(p);
		}

		GD.Print($"Registered player: {p.Name} (ID: {p.Id})");
	}

	public void WhenHumanPlayerReady(Action<Player> cb)
	{
		if (HumanPlayer != null)
			cb(HumanPlayer);
		else
			HumanPlayerReady += cb;
	}

	public Player GetPlayerById(int id) => Players.FirstOrDefault(p => p.Id == id);
}
