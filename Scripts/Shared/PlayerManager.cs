using System.Collections.Generic;
using Godot;

public partial class PlayerManager : Node
{
	public static PlayerManager Instance { get; private set; }

	public Dictionary<long, Player> PlayersToAdd { get; private set; }
	private readonly Dictionary<long, Player> _players = new();

	public Player LocalPlayer { get; set; }
	public Player Authority { get; set; }

	public override void _EnterTree()
	{
		Instance = this;

		PlayersToAdd = new Dictionary<long, Player>();
	}
}
