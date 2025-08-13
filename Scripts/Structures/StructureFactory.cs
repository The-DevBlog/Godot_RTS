using Godot;
using MyEnums;

public partial class StructureFactory : Node
{
	private MyModels _models;
	private Signals _signals;
	public override void _Ready()
	{
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

	public void PlaceStructure(StructureBasePlaceholder placeholder)
	{
		var navRegion = GetNavigationRegion(placeholder);

		// Capture final transform then cleanup placeholder
		var finalXform = placeholder.GlobalTransform;

		var structureScene = _models.Structures[placeholder.StructureType];
		StructureBase structure = structureScene.Instantiate<StructureBase>();
		structure.Init(placeholder.Player);

		navRegion.AddChild(structure, true);
		structure.GlobalTransform = finalXform;

		placeholder.Player.AddStructure(structure);

		// Rebake the navigation mesh AFTER spawning the structure
		_signals.EmitUpdateNavigationMap(navRegion);
	}

	private NavigationRegion3D GetNavigationRegion(StructureBasePlaceholder placeholder)
	{
		// Raycast to find ground hit
		var groundBody = placeholder.GetHoveredMapBase(out Vector3 hitPos);
		if (groundBody == null)
			return null;

		NavigationRegion3D navRegion = null;
		Node walker = groundBody;
		while (walker != null)
		{
			if (walker is NavigationRegion3D nr)
			{
				navRegion = nr;
				break;
			}
			walker = walker.GetParent();
		}

		return navRegion;
	}

	// [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	// private void ServerSpawnStructure(Transform3D finalXform, int structureTypeInt)
	// {
	// 	var structureType = (StructureType)structureTypeInt;
	// 	var structureScene = _models.Structures[structureType];
	// 	StructureBase structure = structureScene.Instantiate<StructureBase>();

	// 	// var spawner = GlobalResources.Instance.MultiplayerSpawner;
	// 	NavigationRegion3D parent = spawner.GetNode<NavigationRegion3D>(spawner.SpawnPath);
	// 	parent.AddChild(structure, true);
	// 	structure.GlobalTransform = finalXform;

	// 	var player = PlayerManager.Instance.LocalPlayer;
	// 	player.AddStructure(structure);
	// }
}
