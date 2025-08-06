using Godot;
using MyEnums;

public partial class MainMenu : Control
{
	private MyScenes _scenes;
	public override void _Ready()
	{
		_scenes = AssetServer.Instance.Scenes;
		Utils.NullCheck(_scenes);
	}

	private void OnSinglePlayerPressed()
	{

	}

	private void OnMultiplayerPressed()
	{
		GetTree().ChangeSceneToPacked(_scenes.Scenes[SceneType.LobbyMenu]);
		// GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.LobbyMenu]);
	}
}
