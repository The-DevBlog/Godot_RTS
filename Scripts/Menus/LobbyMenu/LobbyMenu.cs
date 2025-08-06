using Godot;
using MyEnums;

public partial class LobbyMenu : Control
{
	[Export] private VBoxContainer _playerList;
	[Export] private VBoxContainer _lobbyContainer;
	[Export] private VBoxContainer _hostJoinContainer;
	[Export] private MultiplayerSpawner _multiplayerSpawner;
	private PlayerManager _playerManager;
	private PackedScene _playerContainerScene;
	private MyScenes _scenes;
	private const int ServerPort = 8080;
	private const string ServerIP = "127.0.0.1";

	public Player LocalPlayer { get; private set; }

	public override void _Ready()
	{
		// Load scenes and manager
		_scenes = AssetServer.Instance.Scenes;
		_playerManager = PlayerManager.Instance;
		_playerContainerScene = _scenes.Scenes[SceneType.PlayerContainer];


		// Null checks for exported nodes
		Utils.NullCheck(_scenes);
		Utils.NullExportCheck(_playerList);
		Utils.NullExportCheck(_multiplayerSpawner);
		Utils.NullExportCheck(_lobbyContainer);
		Utils.NullExportCheck(_hostJoinContainer);

		_multiplayerSpawner.SpawnFunction = new Callable(this, nameof(OnPlayerSpawn));
	}

	private void OnHostPressed()
	{
		_hostJoinContainer.Hide();
		_lobbyContainer.Show();

		var serverPeer = new ENetMultiplayerPeer();
		serverPeer.CreateServer(ServerPort);

		Multiplayer.MultiplayerPeer = serverPeer;
		Multiplayer.PeerConnected += AddPlayer;
		Multiplayer.PeerDisconnected += RemovePlayer;

		AddPlayer(Multiplayer.GetUniqueId());
	}

	private void AddPlayer(long id)
	{
		if (!Multiplayer.IsServer()) return;

		// this will call OnPlayerSpawn(id) on server+clients
		_multiplayerSpawner.Spawn(id);
	}

	private Node OnPlayerSpawn(Variant data)
	{
		int id = (int)(long)data;                   // pull your peer ID back out
		var pc = _playerContainerScene.Instantiate<PlayerContainer>();
		pc.Name = $"Player{id}";
		pc.SetMultiplayerAuthority(1);             // authority given to server
		pc.PlayerId = id;                          // exported int, watched by your synchronizer
		return pc;
	}

	private void RemovePlayer(long id)
	{
		if (!Multiplayer.IsServer())
			return;

		var name = $"Player{id}";
		if (_playerList.HasNode(name))
			_playerList.GetNode<Node>(name).QueueFree();

		GD.Print($"Player {id} has left the game!");
	}

	private void OnJoinPressed()
	{
		_hostJoinContainer.Hide();
		_lobbyContainer.Show();

		// Connect to ENet server as client
		var _clientPeer = new ENetMultiplayerPeer();
		_clientPeer.CreateClient(ServerIP, ServerPort);
		Multiplayer.MultiplayerPeer = _clientPeer;

		// Server will spawn us via the spawner
		AddPlayer(Multiplayer.GetUniqueId());
	}

	private void OnLaunchGamePressed()
	{
		GD.Print("Launching game...");

		if (!Multiplayer.IsServer())
			return;

		_playerManager.Authority = _playerManager.LocalPlayer;
		// var spawner = GetNode<MultiplayerSpawner>("PlayerSpawner");

		Rpc(nameof(RpcLaunchGame));
		GetTree().ChangeSceneToPacked(_scenes.Scenes[SceneType.Root]);
		// GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.Root]);

		// _playerManager.SpawnPlayers();
		GD.Print("Game launched successfully!");
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcLaunchGame()
	{
		GetTree().ChangeSceneToPacked(_scenes.Scenes[SceneType.Root]);
		// GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.Root]);
	}

	private void OnLeaveLobbyPressed()
	{
		_hostJoinContainer.Show();
		_lobbyContainer.Hide();

		Multiplayer.MultiplayerPeer.Close();
		Multiplayer.MultiplayerPeer = null;
	}

	private void OnBackToMainMenuPressed()
	{
		GetTree().ChangeSceneToPacked(_scenes.Scenes[SceneType.MainMenu]);
		// GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.MainMenu]);
	}
}
