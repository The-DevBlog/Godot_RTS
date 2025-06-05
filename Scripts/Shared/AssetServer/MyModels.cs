using Godot;
using MyEnums;

public class MyModels
{
    public PackedScene Garage { get; set; }
    public PackedScene Barracks { get; set; }
    public PackedScene OilWell { get; set; }
    public PackedScene Satellite { get; set; }
    public PackedScene Cannon { get; set; }
    public PackedScene ResearchLab { get; set; }
    public PackedScene Generator { get; set; }

    public MyModels()
    {
        Garage = GD.Load<PackedScene>("res://Scenes/Structures/garage.tscn");
        Barracks = GD.Load<PackedScene>("res://Scenes/Structures/barracks.tscn");
        Cannon = GD.Load<PackedScene>("res://Scenes/Structures/cannon.tscn");
        Satellite = GD.Load<PackedScene>("res://Scenes/Structures/satellite.tscn");
        ResearchLab = GD.Load<PackedScene>("res://Scenes/Structures/research_lab.tscn");
        Generator = GD.Load<PackedScene>("res://Scenes/Structures/generator.tscn");
        OilWell = GD.Load<PackedScene>("res://Scenes/Structures/research_lab.tscn");
    }

    public PackedScene GetModel(Structure structure)
    {
        var structureModel = structure switch
        {
            Structure.Barracks => Barracks,
            Structure.Garage => Garage,
            Structure.Cannon => Cannon,
            Structure.Generator => Generator,
            Structure.ResearchLab => ResearchLab,
            Structure.OilWell => OilWell,
            Structure.Satellite => Satellite,
            Structure.None => ResearchLab,
            _ => ResearchLab
        };

        return structureModel;
    }
}