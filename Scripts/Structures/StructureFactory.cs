using Godot;
using MyEnums;

public partial class StructureFactory : Node
{
	public static StructureFactory Instance { get; private set; }
	private Player _player;
	private MyModels _models;
	private Signals _signals;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		// Instance = this;
		_models = AssetServer.Instance.Models;
		_player = PlayerManager.Instance.LocalPlayer;
		_signals = Signals.Instance;

		Utils.NullCheck(_player);
		Utils.NullCheck(_models);
		Utils.NullCheck(_signals);
	}

	public StructureBasePlaceholder BuildPlaceholder(StructureType structureType)
	{
		// Deselect existing selections
		_player.EmitSignal(nameof(_player.DeselectAllUnits));

		// check if enough funds
		StructureBase structure = _models.Structures[structureType].Instantiate<StructureBase>();
		if (_player.Funds < structure.Cost)
		{
			GD.Print("Not enough funds for structure: " + structureType);
			structure.QueueFree();
			return null;
		}
		structure.QueueFree();

		// Deselect existing selections
		_player.EmitSignal(nameof(_player.DeselectAllUnits));

		// Check max structures before placement
		if (_player.MaxStructureCountReached(structureType))
		{
			GD.Print("Max structures for type " + structureType + " reached.");
			return null;
		}

		StructureBasePlaceholder placeholder = _models.StructurePlaceholders[structureType].Instantiate<StructureBasePlaceholder>();
		placeholder.Player = _player;

		_player.IsPlacingStructure = true;

		return placeholder;
	}

	public void PlaceStructure(StructureBasePlaceholder placeholder)
	{
		var navRegion = GetNavigationRegion(placeholder);

		// Capture final transform then cleanup placeholder
		var finalXform = placeholder.GlobalTransform;
		var player = PlayerManager.Instance.LocalPlayer;

		if (Multiplayer.IsServer())
			ServerSpawnStructure(player.Id, finalXform, (int)placeholder.StructureType);
		else
			RpcId(1, nameof(ServerSpawnStructure), player.Id, finalXform, (int)placeholder.StructureType);

		// var structureScene = _models.Structures[placeholder.StructureType];
		// StructureBase structure = structureScene.Instantiate<StructureBase>();
		// player.AddStructure(structure);

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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void ServerSpawnStructure(long playerId, Transform3D finalXform, int structureTypeInt)
	{
		if (!Multiplayer.IsServer())
		{
			// GD.PrintErr("ServerSpawnStructure called on non-server peer.");
			return;
		}

		var structureType = (StructureType)structureTypeInt;
		var structureScene = _models.Structures[structureType];
		StructureBase structure = structureScene.Instantiate<StructureBase>();

		var spawner = Resources.Instance.MultiplayerSpawner;
		NavigationRegion3D parent = spawner.GetNode<NavigationRegion3D>(spawner.SpawnPath);
		parent.AddChild(structure, true);
		structure.GlobalTransform = finalXform;

		// var player = PlayerManager.Instance.LocalPlayer;
		// var player = PlayerManager.Instance.GetPlayer((int)playerId);
		// player.AddStructure(structure);
	}
}
