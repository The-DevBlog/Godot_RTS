using Godot;
using MyEnums;

public partial class LobbyMenu : Control
{
	public Player LocalPlayer { get; private set; }
	public PackedScene _playerScene;
	[Export] private VBoxContainer _playerList;
	[Export] private PanelContainer _playerContainer;
	[Export] private VBoxContainer _lobbyContainer;
	[Export] private VBoxContainer _hostJoinContainer;
	private PlayerManager _playerManager;
	private const int ServerPort = 8080;
	private const string ServerIP = "127.0.0.1";
	private MyScenes _scenes;
	public override void _Ready()
	{
		_scenes = AssetServer.Instance.Scenes;
		_playerManager = PlayerManager.Instance;
		_playerScene = GD.Load<PackedScene>(AssetServer.Instance.Scenes.Scenes[SceneType.Player]);

		Utils.NullCheck(_scenes);
		Utils.NullExportCheck(_playerContainer);
		Utils.NullExportCheck(_playerList);
		Utils.NullExportCheck(_lobbyContainer);
		Utils.NullExportCheck(_hostJoinContainer);
	}

	private void OnLaunchGame()
	{
		GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.Root]);
	}

	private void OnHostPressed()
	{
		_hostJoinContainer.Hide();
		_lobbyContainer.Show();

		var serverPeer = new ENetMultiplayerPeer();
		serverPeer.CreateServer(ServerPort);

		Multiplayer.MultiplayerPeer = serverPeer;
		Multiplayer.PeerConnected += AddPlayer;
		// Multiplayer.PeerDisconnected += RemovePlayerFromGame;

		LocalPlayer = _playerScene.Instantiate<Player>();
		_playerManager.LocalPlayer = LocalPlayer;

		AddPlayer(Multiplayer.GetUniqueId());
		// AddPlayerToLobby(Multiplayer.GetUniqueId());
		// Rpc(nameof(AddPlayerToLobby), Multiplayer.GetUniqueId());
	}

	private void AddPlayer(long id)
	{
		GD.Print($"Player {id} has joined the game!");

		var newPlayer = _playerScene.Instantiate<Player>();
		newPlayer.Id = (int)id;
		newPlayer.Name = id.ToString();
		_playerManager.PlayersToAdd[id] = newPlayer;
		// _playersSpawnNode.AddChild(newPlayer, true);

		// 1) show the new player on this host’s UI
		AddPlayerToLobby(id);

		// 2) broadcast to everyone else
		Rpc(nameof(AddPlayerToLobby), id);

		// 3) now “catch up” the new client by sending them all prior players
		foreach (var kv in _playerManager.PlayersToAdd)
		{
			long existingId = kv.Key;
			if (existingId == id) continue;
			// send only to the newcomer
			RpcId(id, nameof(AddPlayerToLobby), existingId);
		}

		GD.Print("Connected players: ", _playerManager.PlayersToAdd.Count);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void AddPlayerToLobby(long id)
	{
		PanelContainer row = (PanelContainer)_playerContainer.Duplicate();
		row.Name = $"Player{id}";
		row.GetNode<Label>("HBoxContainer/PlayerLabel").Text = $"Player {id}";
		row.Show();
		_playerList.AddChild(row);
	}

	private void OnJoinPressed()
	{
		_hostJoinContainer.Hide();
		_lobbyContainer.Show();

		var clientPeer = new ENetMultiplayerPeer();
		clientPeer.CreateClient(ServerIP, ServerPort);

		Multiplayer.MultiplayerPeer = clientPeer;
		LocalPlayer = _playerScene.Instantiate<Player>();
		_playerManager.LocalPlayer = LocalPlayer;
	}

	private void OnBackToHostJoinPressed()
	{
		_hostJoinContainer.Show();
		_lobbyContainer.Hide();
	}

	private void OnBackToMainMenuPressed()
	{
		GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.MainMenu]);
	}
}
