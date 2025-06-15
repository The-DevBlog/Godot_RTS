using System.Collections.Generic;
using Godot;
using MyEnums;

public class MyModels
{
    public Dictionary<StructureType, PackedScene> Structures { get; set; }
    public Dictionary<StructureType, PackedScene> StructurePlaceholders { get; set; }

    public MyModels()
    {
        Structures = new Dictionary<StructureType, PackedScene>
        {
            { StructureType.Barracks,    GD.Load<PackedScene>("res://Scenes/Structures/barracks.tscn") },
            { StructureType.Garage,      GD.Load<PackedScene>("res://Scenes/Structures/garage.tscn")   },
            { StructureType.Cannon,      GD.Load<PackedScene>("res://Scenes/Structures/cannon.tscn")   },
            { StructureType.Generator,   GD.Load<PackedScene>("res://Scenes/Structures/generator.tscn")},
            { StructureType.Reactor,     GD.Load<PackedScene>("res://Scenes/Structures/reactor.tscn")},
            { StructureType.ResearchLab, GD.Load<PackedScene>("res://Scenes/Structures/research_lab.tscn") },
            { StructureType.ScrapYard,   GD.Load<PackedScene>("res://Scenes/Structures/scrap_yard.tscn")  },
            { StructureType.Satellite,   GD.Load<PackedScene>("res://Scenes/Structures/satellite.tscn") },
        };

        StructurePlaceholders = new Dictionary<StructureType, PackedScene>
        {
            { StructureType.Barracks,    GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/barracks_placeholder.tscn") },
            { StructureType.Garage,      GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/garage_placeholder.tscn")   },
            { StructureType.Cannon,      GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/cannon_placeholder.tscn")   },
            { StructureType.Generator,   GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/generator_placeholder.tscn")},
            { StructureType.Reactor,     GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/reactor_placeholder.tscn")},
            { StructureType.ResearchLab, GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/research_lab_placeholder.tscn") },
            { StructureType.ScrapYard,   GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/scrap_yard_placeholder.tscn")  },
            { StructureType.Satellite,   GD.Load<PackedScene>("res://Scenes/Structures/Placeholders/satellite_placeholder.tscn") },
        };
    }
}
