using Godot;
using MyEnums;

public partial class LobbyMenu : Control
{
	[Export] private VBoxContainer _playerList;
	[Export] private PanelContainer _playerContainer;
	[Export] private VBoxContainer _lobbyContainer;
	[Export] private VBoxContainer _hostJoinContainer;
	private MyScenes _scenes;
	public override void _Ready()
	{
		_scenes = AssetServer.Instance.Scenes;

		Utils.NullCheck(_scenes);
		Utils.NullExportCheck(_playerContainer);
		Utils.NullExportCheck(_playerList);
		Utils.NullExportCheck(_lobbyContainer);
		Utils.NullExportCheck(_hostJoinContainer);
	}

	private void AddPlayer()
	{
		PanelContainer newPlayer = (PanelContainer)_playerContainer.Duplicate();
		newPlayer.Name = $"Player{_playerList.GetChildCount() + 1}";
		newPlayer.GetNode<Label>("HBoxContainer/PlayerLabel").Text = $"Player {_playerList.GetChildCount() + 1}";
		_playerList.AddChild(newPlayer);
	}

	private void OnLaunchGame()
	{
		GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.Root]);
	}

	private void OnHostPressed()
	{
		_hostJoinContainer.Hide();
		_lobbyContainer.Show();
	}

	private void OnJoinPressed()
	{
		_hostJoinContainer.Hide();
		_lobbyContainer.Show();

		AddPlayer();
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
