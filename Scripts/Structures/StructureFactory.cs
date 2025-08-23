using Godot;
using MyEnums;

public partial class StructureFactory : Node
{
	[Export] private NavigationRegion3D _navRegion;
	private MyModels _models;
	private Signals _signals;
	public override void _Ready()
	{
		Utils.NullExportCheck(_navRegion);

		_models = AssetServer.Instance.Models;
		_signals = Signals.Instance;
	}

	public StructureBasePlaceholder BuildPlaceholder(Player player, StructureType structureType)
	{
		// Deselect existing selections
		player.EmitSignal(nameof(player.DeselectAllUnits));

		// check if enough funds
		StructureBase structure = _models.Structures[structureType].Instantiate<StructureBase>();
		if (player.Funds < structure.Cost)
		{
			GD.Print("Not enough funds for structure: " + structureType);
			structure.QueueFree();
			return null;
		}
		structure.QueueFree();

		// Deselect existing selections
		player.EmitSignal(nameof(player.DeselectAllUnits));

		// Check max structures before placement
		if (player.MaxStructureCountReached(structureType))
		{
			GD.Print("Max structures for type " + structureType + " reached.");
			return null;
		}

		StructureBasePlaceholder placeholder = _models.StructurePlaceholders[structureType].Instantiate<StructureBasePlaceholder>();
		placeholder.Player = player;

		GlobalResources.Instance.IsPlacingStructure = true;

		return placeholder;
	}

	public void PlaceStructure(StructureBasePlaceholder placeholder, Player player)
	{
		// Capture final transform then cleanup placeholder
		var finalXform = placeholder.GlobalTransform;

		var structureScene = _models.Structures[placeholder.StructureType];
		StructureBase structure = structureScene.Instantiate<StructureBase>();
		structure.Init(placeholder.Player);

		GD.Print("Structure: " + structure);
		GD.Print("Nav region: " + _navRegion);

		_navRegion.AddChild(structure, true);
		structure.GlobalTransform = finalXform;

		player.UpdateFunds(-structure.Cost);

		// Rebake the navigation mesh AFTER spawning the structure
		_signals.EmitUpdateNavigationMap(_navRegion);
	}
}
