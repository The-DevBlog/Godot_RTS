using Godot;
using MyEnums;

public partial class MultiplayerTest : Control
{
	public Player LocalPlayer { get; private set; }
	public PackedScene _playerScene;
	[Export] private PackedScene _scene = new PackedScene();
	[Export] private Node3D _playersSpawnNode;
	private ENetMultiplayerPeer _peer = new ENetMultiplayerPeer();
	private PlayerManager _playerManager;
	private const int ServerPort = 8080;
	private const string ServerIP = "127.0.0.1";
	public override void _Ready()
	{
		_playerManager = PlayerManager.Instance;
		_playerScene = GD.Load<PackedScene>(AssetServer.Instance.Scenes.Scenes[SceneType.Player]);

		Utils.NullExportCheck(_playersSpawnNode);
	}

	public void OnHostPressed()
	{
		GD.Print("Starting host!");

		var serverPeer = new ENetMultiplayerPeer();
		serverPeer.CreateServer(ServerPort);

		Multiplayer.MultiplayerPeer = serverPeer;
		Multiplayer.PeerConnected += AddPlayerToGame;
		Multiplayer.PeerDisconnected += RemovePlayerFromGame;

		LocalPlayer = _playerScene.Instantiate<Player>();

		Hide();
		AddPlayerToGame(1);
	}

	public void OnJoinPressed()
	{
		var clientPeer = new ENetMultiplayerPeer();
		clientPeer.CreateClient(ServerIP, ServerPort);

		Multiplayer.MultiplayerPeer = clientPeer;
		LocalPlayer = _playerScene.Instantiate<Player>();

		Hide();
	}

	private void AddPlayerToGame(long id)
	{
		GD.Print($"Player {id} has joined the game!");

		var newPlayer = _playerScene.Instantiate<Player>();
		newPlayer.Id = (int)id;
		newPlayer.Name = id.ToString();
		_playersSpawnNode.AddChild(newPlayer, true);
	}

	private void RemovePlayerFromGame(long id)
	{
		GD.Print($"Player {id} has left the game!");

		if (_playersSpawnNode.HasNode(id.ToString()))
			_playersSpawnNode.GetNode(id.ToString()).QueueFree();
	}
}
