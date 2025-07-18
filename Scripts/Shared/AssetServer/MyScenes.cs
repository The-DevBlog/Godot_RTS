using System.Collections.Generic;
using MyEnums;

public class MyScenes
{
    public Dictionary<SceneType, string> Scenes { get; set; }
    public MyScenes()
    {
        Scenes = new Dictionary<SceneType, string>
        {
            [SceneType.Root] = "res://Scenes/root.tscn",
            [SceneType.Player] = "res://Scenes/player.tscn",
            [SceneType.LobbyMenu] = "res://Scenes/Menus/multiplayer_lobby.tscn",
            [SceneType.MainMenu] = "res://Scenes/Menus/main_menu.tscn",
        };
    }
}
