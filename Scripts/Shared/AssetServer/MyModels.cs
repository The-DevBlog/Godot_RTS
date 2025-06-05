using System.Collections.Generic;
using Godot;
using MyEnums;

public class MyModels
{
    public Dictionary<Structure, PackedScene> Models { get; set; }

    public MyModels()
    {
        Models = new Dictionary<Structure, PackedScene>
        {
            { Structure.Barracks,    GD.Load<PackedScene>("res://Scenes/Structures/barracks.tscn") },
            { Structure.Garage,      GD.Load<PackedScene>("res://Scenes/Structures/garage.tscn")   },
            { Structure.Cannon,      GD.Load<PackedScene>("res://Scenes/Structures/cannon.tscn")   },
            { Structure.Generator,   GD.Load<PackedScene>("res://Scenes/Structures/generator.tscn")},
            { Structure.ResearchLab, GD.Load<PackedScene>("res://Scenes/Structures/research_lab.tscn") },
            { Structure.OilWell,     GD.Load<PackedScene>("res://Scenes/Structures/oil_well.tscn")  },
            { Structure.Satellite,   GD.Load<PackedScene>("res://Scenes/Structures/satellite.tscn") },
        };
    }
}
