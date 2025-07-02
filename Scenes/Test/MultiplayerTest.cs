using Godot;

public partial class MultiplayerTest : Node2D
{
	[Export] private PackedScene _scene = new PackedScene();
	private ENetMultiplayerPeer _peer = new ENetMultiplayerPeer();
	private PlayerManager _playerManager;
	// private NetworkManager _networkManager;
	public override void _Ready()
	{
		_playerManager = PlayerManager.Instance;
		// _networkManager = NetworkManager.Instance;
	}

	// public void OnJoin()
	// {
	// 	_networkManager.ConnectToServer("localhost");
	// }

	// public void OnHost()
	// {
	// 	GD.Print(_networkManager);
	// 	_networkManager.StartServer();
	// }

	public void OnHostPressed()
	{
		_peer.CreateServer(135);
		Multiplayer.MultiplayerPeer = _peer;
		Multiplayer.PeerConnected += AddPlayer;
		AddPlayer();
	}

	public void OnJoinPressed()
	{
		_peer.CreateClient("localhost", 135);
		Multiplayer.MultiplayerPeer = _peer;
	}

	private void AddPlayer(long id = 1)
	{
		GD.Print($"Adding player with ID: {id}");

		var player = _scene.Instantiate() as Player; // assuming your scene is a Player
		player.Name = id.ToString();

		// VERY IMPORTANT: set multiplayer authority
		player.SetMultiplayerAuthority((int)id);

		// Now if this player belongs to us, set it as our authority
		if (id == Multiplayer.GetUniqueId())
		{
			_playerManager.Authority = player;
			GD.Print("Set local authority player");
		}

		CallDeferred("add_child", player);
	}


	// private void AddPlayer(long id = 1)
	// {
	// 	GD.Print($"Adding player with ID: {id}");
	// 	var player = _scene.Instantiate();
	// 	player.Name = id.ToString();

	// 	_playerManager.Authority = player as Player;

	// 	CallDeferred("add_child", player);
	// }
}
