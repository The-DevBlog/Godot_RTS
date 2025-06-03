using Godot;

public class MyModels
{
    public PackedScene Garage { get; set; }

    public MyModels()
    {
        Garage = GD.Load<PackedScene>("res://Scenes/Structures/garage.tscn");
    }
}