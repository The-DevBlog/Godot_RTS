using Godot;

public partial class MultiplayerTest : Node2D
{
	// [Export] private PackedScene _scene = new PackedScene();
	// private ENetMultiplayerPeer _peer = new ENetMultiplayerPeer();
	private NetworkManager _networkManager;
	public override void _Ready()
	{
		_networkManager = NetworkManager.Instance;
	}

	public void OnJoin()
	{
		_networkManager.ConnectToServer("localhost");
	}

	public void OnHost()
	{
		GD.Print(_networkManager);
		_networkManager.StartServer();
	}

	// public void OnHostPressed()
	// {
	// 	_peer.CreateServer(135);
	// 	Multiplayer.MultiplayerPeer = _peer;
	// 	Multiplayer.PeerConnected += AddPlayer;
	// 	AddPlayer();
	// }

	// public void OnJoinPressed()
	// {
	// 	_peer.CreateClient("localhost", 135);
	// 	Multiplayer.MultiplayerPeer = _peer;
	// }

	// private void AddPlayer(long id = 1)
	// {
	// 	var player = _scene.Instantiate();
	// 	player.Name = id.ToString();
	// 	CallDeferred("add_child", player);
	// }
}
