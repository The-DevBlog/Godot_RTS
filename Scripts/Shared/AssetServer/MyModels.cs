using System.Collections.Generic;
using Godot;
using MyEnums;

public class MyModels
{
    public Dictionary<StructureType, PackedScene> Structures { get; set; }
    public Dictionary<StructureType, PackedScene> StructurePlaceholders { get; set; }
    public Dictionary<InfantryType, PackedScene> Infantry { get; set; }
    public Dictionary<VehicleType, PackedScene> Vehicles { get; set; }
    public Dictionary<WeaponType, PackedScene> Projectiles { get; set; }
    public Dictionary<LODScenes, PackedScene> LODs { get; set; }

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

        Infantry = new Dictionary<InfantryType, PackedScene>
        {
            { InfantryType.Infantry, GD.Load<PackedScene>("res://Scenes/Units/Infantry.tscn") },
        };

        Vehicles = new Dictionary<VehicleType, PackedScene>
        {
            { VehicleType.TankGen1, GD.Load<PackedScene>("res://Scenes/Units/tank_gen_1.tscn") },
            { VehicleType.TankGen2, GD.Load<PackedScene>("res://Scenes/Units/tank_gen_2.tscn") },
            { VehicleType.Artillery, GD.Load<PackedScene>("res://Scenes/Units/artillery.tscn") },
            { VehicleType.AntiInfantry, GD.Load<PackedScene>("res://Scenes/Units/anti_infantry.tscn") },
            { VehicleType.BFT, GD.Load<PackedScene>("res://Scenes/Units/BFT.tscn") }
        };

        // Used in CombatSystem.TryAttack() to create the projectile
        Projectiles = new Dictionary<WeaponType, PackedScene>
        {
            { WeaponType.Cannon, GD.Load<PackedScene>("res://Scenes/Projectiles/tank_projectile.tscn") },
            { WeaponType.SmallArms, GD.Load<PackedScene>("res://Scenes/Projectiles/tracer.tscn") },
        };

        LODs = new Dictionary<LODScenes, PackedScene>
        {
            { LODScenes.AntiInfantryLP, GD.Load<PackedScene>("res://Scenes/Units/LOD/AntiInfantry/anti_infantry_lp.tscn") },
            { LODScenes.AntiInfantryHP, GD.Load<PackedScene>("res://Scenes/Units/LOD/AntiInfantry/anti_infantry_hp.tscn") },
            { LODScenes.TankGen2HP, GD.Load<PackedScene>("res://Scenes/Units/LOD/TankGen2/tank_gen_2_hp.tscn") },
            { LODScenes.TankGen2LP, GD.Load<PackedScene>("res://Scenes/Units/LOD/TankGen2/tank_gen_2_lp.tscn") },
            { LODScenes.BftLP, GD.Load<PackedScene>("res://Scenes/Units/LOD/BFT/bft_lp.tscn") },
            { LODScenes.BftHP, GD.Load<PackedScene>("res://Scenes/Units/LOD/BFT/bft_hp.tscn") }
        };
    }
}
