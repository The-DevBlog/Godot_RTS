using System.Collections.Generic;
using Godot;
using MyEnums;

public class MyScenes
{
    public Dictionary<SceneType, PackedScene> Scenes { get; set; }
    public MyScenes()
    {
        Scenes = new Dictionary<SceneType, PackedScene>
        {
            { SceneType.Root,            GD.Load<PackedScene>("res://Scenes/root.tscn") },
            { SceneType.Player,          GD.Load<PackedScene>("res://Scenes/player.tscn") },
            { SceneType.LobbyMenu,       GD.Load<PackedScene>("res://Scenes/Menus/multiplayer_lobby.tscn") },
            { SceneType.PlayerContainer, GD.Load<PackedScene>("res://Scenes/Menus/player_container.tscn") },
            { SceneType.MainMenu,        GD.Load<PackedScene>("res://Scenes/Menus/main_menu.tscn") }
        };
    }
}
