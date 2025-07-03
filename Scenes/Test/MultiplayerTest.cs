using Godot;

public partial class MultiplayerTest : Node2D
{
	[Export] private PackedScene _scene = new PackedScene();
	private ENetMultiplayerPeer _peer = new ENetMultiplayerPeer();
	private PlayerManager _playerManager;
	public override void _Ready()
	{
		_playerManager = PlayerManager.Instance;
	}

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

		Player player = _scene.Instantiate() as Player; // assuming your scene is a Player
		player.Name = id.ToString();

		// VERY IMPORTANT: set multiplayer authority
		player.SetMultiplayerAuthority((int)id);

		_playerManager.AddPlayer(id, player);
		CallDeferred("add_child", player);
	}
}
