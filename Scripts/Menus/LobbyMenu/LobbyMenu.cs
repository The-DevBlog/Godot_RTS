// using Godot;
// using MyEnums;

// public partial class LobbyMenu : Control
// {
// 	public Player LocalPlayer { get; private set; }
// 	public PackedScene _playerScene;
// 	[Export] private VBoxContainer _playerList;
// 	[Export] private PanelContainer _playerContainer;
// 	[Export] private VBoxContainer _lobbyContainer;
// 	[Export] private VBoxContainer _hostJoinContainer;
// 	[Export] private MultiplayerSpawner _multiplayerSpawner;
// 	private PlayerManager _playerManager;
// 	private const int ServerPort = 8080;
// 	private const string ServerIP = "127.0.0.1";
// 	private MyScenes _scenes;
// 	public override void _Ready()
// 	{
// 		_scenes = AssetServer.Instance.Scenes;
// 		_playerManager = PlayerManager.Instance;
// 		_playerScene = GD.Load<PackedScene>(_scenes.Scenes[SceneType.Player]);

// 		Utils.NullCheck(_scenes);
// 		Utils.NullExportCheck(_playerContainer);
// 		Utils.NullExportCheck(_playerList);
// 		Utils.NullExportCheck(_lobbyContainer);
// 		Utils.NullExportCheck(_hostJoinContainer);
// 		Utils.NullExportCheck(_multiplayerSpawner);
// 	}

// 	private void OnLaunchGame()
// 	{
// 		GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.Root]);
// 	}

// 	private void OnHostPressed()
// 	{
// 		_hostJoinContainer.Hide();
// 		_lobbyContainer.Show();

// 		var serverPeer = new ENetMultiplayerPeer();
// 		serverPeer.CreateServer(ServerPort);

// 		Multiplayer.MultiplayerPeer = serverPeer;
// 		Multiplayer.PeerConnected += AddPlayer;
// 		// Multiplayer.PeerDisconnected += RemovePlayerFromGame;

// 		LocalPlayer = _playerScene.Instantiate<Player>();
// 		_playerManager.LocalPlayer = LocalPlayer;

// 		AddPlayer(Multiplayer.GetUniqueId());
// 		// AddPlayerToLobby(Multiplayer.GetUniqueId());
// 		// Rpc(nameof(AddPlayerToLobby), Multiplayer.GetUniqueId());
// 	}

// 	private void AddPlayer(long id)
// 	{
// 		// var newPlayer = playerContainer.Instantiate<Player>();

// 		// GD.Print($"Player {id} has joined the game!");

// 		var newPlayer = _playerScene.Instantiate<Player>();
// 		newPlayer.Id = (int)id;
// 		newPlayer.Name = id.ToString();
// 		_playerManager.PlayersToAdd[id] = newPlayer;

// 		_multiplayerSpawner.Spawn();


// 		// var playerContainer = _scenes.Scenes[SceneType.PlayerContainer];
// 		// var playerContainer = GD.Load<PackedScene>(_scenes.Scenes[SceneType.PlayerContainer]);
// 		// var row = (PanelContainer)playerContainer.Instantiate();
// 		// row.Name = $"Player{id}";
// 		// row.GetNode<Label>("HBoxContainer/PlayerLabel").Text = $"Player {id}";
// 		// row.Show();
// 		// _playerList.AddChild(row);

// 		// var row = (PanelContainer)playerContainer.In


// 		// _playersSpawnNode.AddChild(newPlayer, true);

// 		// // 1) show the new player on this host’s UI
// 		// AddPlayerToLobby(id);

// 		// // 2) broadcast to everyone else
// 		// Rpc(nameof(AddPlayerToLobby), id);

// 		// // 3) now “catch up” the new client by sending them all prior players
// 		// foreach (var kv in _playerManager.PlayersToAdd)
// 		// {
// 		// 	long existingId = kv.Key;
// 		// 	if (existingId == id) continue;
// 		// 	// send only to the newcomer
// 		// 	RpcId(id, nameof(AddPlayerToLobby), existingId);
// 		// }

// 		// GD.Print("Connected players: ", _playerManager.PlayersToAdd.Count);
// 	}

// 	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
// 	private void AddPlayerToLobby(long id)
// 	{
// 		PanelContainer row = (PanelContainer)_playerContainer.Duplicate();
// 		row.Name = $"Player{id}";
// 		row.GetNode<Label>("HBoxContainer/PlayerLabel").Text = $"Player {id}";
// 		row.Show();
// 		_playerList.AddChild(row);
// 	}

// 	private void OnJoinPressed()
// 	{
// 		_hostJoinContainer.Hide();
// 		_lobbyContainer.Show();

// 		var clientPeer = new ENetMultiplayerPeer();
// 		clientPeer.CreateClient(ServerIP, ServerPort);

// 		Multiplayer.MultiplayerPeer = clientPeer;
// 		LocalPlayer = _playerScene.Instantiate<Player>();
// 		_playerManager.LocalPlayer = LocalPlayer;
// 	}

// 	private void OnBackToHostJoinPressed()
// 	{
// 		_hostJoinContainer.Show();
// 		_lobbyContainer.Hide();
// 	}

// 	private void OnBackToMainMenuPressed()
// 	{
// 		GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.MainMenu]);
// 	}
// }

using Godot;
using MyEnums;

public partial class LobbyMenu : Control
{
	[Export] private VBoxContainer _playerList;
	[Export] private PackedScene _playerContainerScene;
	[Export] private VBoxContainer _lobbyContainer;
	[Export] private VBoxContainer _hostJoinContainer;
	private PlayerManager _playerManager;
	private MyScenes _scenes;
	private const int ServerPort = 8080;
	private const string ServerIP = "127.0.0.1";

	public Player LocalPlayer { get; private set; }

	public override void _Ready()
	{
		// Load scenes and manager
		_scenes = AssetServer.Instance.Scenes;
		_playerManager = PlayerManager.Instance;
		_playerContainerScene = GD.Load<PackedScene>(_scenes.Scenes[SceneType.PlayerContainer]);

		// Null checks for exported nodes
		Utils.NullCheck(_scenes);
		Utils.NullExportCheck(_playerList);
		// Utils.NullExportCheck(_playerContainerScene);
		Utils.NullExportCheck(_lobbyContainer);
		Utils.NullExportCheck(_hostJoinContainer);
	}

	private void OnHostPressed()
	{
		_hostJoinContainer.Hide();
		_lobbyContainer.Show();

		// Start ENet server
		var serverPeer = new ENetMultiplayerPeer();
		serverPeer.CreateServer(ServerPort);
		Multiplayer.MultiplayerPeer = serverPeer;

		// Hook connection signal
		Multiplayer.PeerConnected += AddPlayer;
		Multiplayer.PeerDisconnected += RemovePlayer;

		// Spawn the host player (peer 1)
		AddPlayer(Multiplayer.GetUniqueId());
	}

	private void AddPlayer(long id)
	{
		// Only the host actually creates the UI row
		if (!Multiplayer.IsServer())
			return;

		// 1) Instantiate the PanelContainer scene (the one you added in the editor)
		PlayerContainer playerContainer = _playerContainerScene.Instantiate<PlayerContainer>();

		// 2) Assign authority to the new peer so replication works
		playerContainer.SetMultiplayerAuthority((int)id);

		// 3) Tweak the row’s label & name
		playerContainer.Name = $"Player{id}";

		// 4) Add it under the watched node (_playerList_, your Spawn Path)
		_playerList.AddChild(playerContainer, true);
		playerContainer.PlayerIdLabel.Text = $"Player {id}";

		GD.Print($"Player {id} has joined the game!");
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

	private void OnLeaveLobbyPressed()
	{
		_hostJoinContainer.Show();
		_lobbyContainer.Hide();

		Multiplayer.MultiplayerPeer.Close();
		Multiplayer.MultiplayerPeer = null;
	}

	private void OnBackToMainMenuPressed()
	{
		GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.MainMenu]);
	}
}
