using System.Collections.Generic;
using Godot;
using MyEnums;

public class MyTextures
{
    public Dictionary<InfantryType, Texture2D> Infantry { get; set; }
    public Dictionary<VehicleType, Texture2D> Vehicles { get; set; }

    public MyTextures()
    {
        Infantry = new Dictionary<InfantryType, Texture2D>
        {
            { InfantryType.Infantry, GD.Load<Texture2D>("res://Assets/Textures/UI/Units/Infantry.png") },
        };

        Vehicles = new Dictionary<VehicleType, Texture2D>
        {
            { VehicleType.TankGen1, GD.Load<Texture2D>("res://Assets/Textures/UI/Units/TankGen1.png") },
            { VehicleType.TankGen2, GD.Load<Texture2D>("res://Assets/Textures/UI/Units/TankGen2.png") },
            { VehicleType.Artillery, GD.Load<Texture2D>("res://Assets/Textures/UI/Units/Artillery.png") },
        };
    }
}
