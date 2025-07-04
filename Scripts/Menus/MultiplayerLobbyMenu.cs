using Godot;
using MyEnums;

public partial class MultiplayerLobbyMenu : Control
{
	[Export] public string NextScenePath = "res://Scenes/root.tscn";
	private MyScenes _scenes;
	public override void _Ready()
	{
		_scenes = AssetServer.Instance.Scenes;
	}

	private void EnterGame()
	{
		GetTree().ChangeSceneToFile(_scenes.Scenes[SceneType.Root]);
	}
}
