using Godot;
using MyEnums;

public partial class MultiplayerLobbyMenu : Control
{
	private MyScenes _scenes;
	public override void _Ready()
	{
		GD.Print("MultiplayerLobbyMenu _Ready()");
		_scenes = AssetServer.Instance.Scenes;
	}

	private void EnterGame()
	{
		GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.Root]);
	}
}
