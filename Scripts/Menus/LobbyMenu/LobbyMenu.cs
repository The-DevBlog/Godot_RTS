using Godot;
using MyEnums;

public partial class LobbyMenu : Control
{
	[Export] private VBoxContainer _playerList;
	[Export] private PanelContainer _playerContainer;
	private MyScenes _scenes;
	public override void _Ready()
	{
		GD.Print("LobbyMenu _Ready()");
		_scenes = AssetServer.Instance.Scenes;

		Utils.NullCheck(_scenes);
		Utils.NullExportCheck(_playerContainer);
		Utils.NullExportCheck(_playerList);
	}

	private void AddPlayer()
	{
		PanelContainer newPlayer = (PanelContainer)_playerContainer.Duplicate((int)DuplicateFlags.UseInstantiation);
		// newPlayer.MakeSubresourcesUnique();
		newPlayer.Name = $"Player{_playerList.GetChildCount() + 1}";
		newPlayer.GetNode<Label>("HBoxContainer/PlayerLabel").Text = $"Player {_playerList.GetChildCount() + 1}";
		_playerList.AddChild(newPlayer);
	}

	private void EnterGame()
	{
		GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.Root]);
	}
}
