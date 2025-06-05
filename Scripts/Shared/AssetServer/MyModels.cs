using System.Collections.Generic;
using Godot;
using MyEnums;

public class MyModels
{
    public Dictionary<Structure, PackedScene> Structures { get; set; }
    public Dictionary<Structure, PackedScene> StructurePlaceholders { get; set; }

    public MyModels()
    {
        Structures = new Dictionary<Structure, PackedScene>
        {
            { Structure.Barracks,    GD.Load<PackedScene>("res://Scenes/Structures/barracks.tscn") },
            { Structure.Garage,      GD.Load<PackedScene>("res://Scenes/Structures/garage.tscn")   },
            { Structure.Cannon,      GD.Load<PackedScene>("res://Scenes/Structures/cannon.tscn")   },
            { Structure.Generator,   GD.Load<PackedScene>("res://Scenes/Structures/generator.tscn")},
            { Structure.ResearchLab, GD.Load<PackedScene>("res://Scenes/Structures/research_lab.tscn") },
            { Structure.OilWell,     GD.Load<PackedScene>("res://Scenes/Structures/oil_well.tscn")  },
            { Structure.Satellite,   GD.Load<PackedScene>("res://Scenes/Structures/satellite.tscn") },
        };

        StructurePlaceholders = new Dictionary<Structure, PackedScene>
        {
            { Structure.Barracks,    GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/barracks_placeholder.tscn") },
            { Structure.Garage,      GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/garage_placeholder.tscn")   },
            { Structure.Cannon,      GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/cannon_placeholder.tscn")   },
            { Structure.Generator,   GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/generator_placeholder.tscn")},
            { Structure.ResearchLab, GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/research_lab_placeholder.tscn") },
            { Structure.OilWell,     GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/oil_well_placeholder.tscn")  },
            { Structure.Satellite,   GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/satellite_placeholder.tscn") },
        };
    }
}
