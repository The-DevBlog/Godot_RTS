using Godot;
using MyEnums;

public partial class StructureBtn : Button
{
	[Export] public StructureType Structure { get; set; }

	private Player _player;
	private Signals _signals;
	private StructureBasePlaceholder _placeholder;
	private MyModels _models;
	private Camera3D _camera;
	private Node3D _scene;
	private MultiplayerSpawner _spawner;

	public override void _Ready()
	{
		_spawner = GlobalResources.Instance.MultiplayerSpawner;

		// Grab the local Player from the PlayerManager
		_player = PlayerManager.Instance.LocalPlayer;
		if (_player == null)
			GD.PrintErr("[StructureBtn] No local player found!");

		_signals = Signals.Instance;
		_models = AssetServer.Instance.Models;
		_camera = GetViewport().GetCamera3D();
		_scene = GetTree().CurrentScene as Node3D;

		Pressed += OnStructureSelect;
		MouseEntered += OnBtnEnter;
		MouseExited += OnBtnExit;

		if (Structure == StructureType.None)
			Utils.PrintErr($"Structure enum not set on {Name}");

		if (_scene == null)
			Utils.PrintErr("Current scene root is not a Node3D.");
	}

	private void OnStructureSelect()
	{
		// Cancel if already placing
		if (_placeholder != null)
		{
			CancelStructure();
			return;
		}

		// Deselect existing selections
		_player.EmitSignal(nameof(_player.DeselectAllUnits));
		// _signals.EmitSignal(nameof(_player.DeselectAllUnits));

		// Check max structures before placement
		if (_player.MaxStructureCountReached(Structure))
			return;

		// Quick cost check (will be re-checked in SpawnStructure)
		var tempCheck = _models.Structures[Structure].Instantiate<StructureBase>();
		if (_player.Funds < tempCheck.Cost)
		{
			GD.Print("Not enough funds to place " + Structure);
			tempCheck.QueueFree();
			return;
		}
		tempCheck.QueueFree();

		// Create the placement placeholder
		_placeholder = _models.StructurePlaceholders[Structure].Instantiate<StructureBasePlaceholder>();
		_placeholder.Player = _player;
		GlobalResources.Instance.IsPlacingStructure = true;
		_scene.AddChild(_placeholder);
		ReleaseFocus();
	}

	public override void _Input(InputEvent @event)
	{
		if (_placeholder == null)
			return;

		if (Input.IsActionJustPressed("mb_secondary"))
		{
			CancelStructure();
			return;
		}

		if (Input.IsActionJustPressed("mb_primary"))
		{
			PlaceStructure();
			return;
		}

		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			RotatePlaceholder(mb.ButtonIndex == MouseButton.WheelUp ? 90f : -90f);
		}
	}


	private void PlaceStructure()
	{
		if (GlobalResources.Instance.IsHoveringUI)
		{
			CancelStructure();
			return;
		}

		if (_placeholder == null || !_placeholder.ValidPlacement)
			return;

		// 1) Raycast & find navRegion
		var groundBody = _placeholder.GetHoveredMapBase(out Vector3 hitPos);
		if (groundBody == null)
		{
			GD.PrintErr("PlaceStructure: Mouse not over map. Cancelling.");
			CancelStructure();
			return;
		}

		NavigationRegion3D navRegion = null;
		Node walker = groundBody;
		while (walker != null)
		{
			if (walker is NavigationRegion3D nr) { navRegion = nr; break; }
			walker = walker.GetParent();
		}
		if (navRegion == null)
		{
			GD.PrintErr("PlaceStructure: No NavigationRegion3D parent found.");
			CancelStructure();
			return;
		}

		// 2) Capture final transform & cleanup placeholder
		var finalXform = _placeholder.GlobalTransform;
		CancelStructure();

		// 3) Figure out which entry in the spawner’s Auto Spawn List this is
		string scenePath = _models.Structures[Structure].ResourcePath;
		int sceneIdx = -1;
		for (int i = 0; i < _spawner.GetSpawnableSceneCount(); i++)
		{
			if (_spawner.GetSpawnableScene(i) == scenePath)
			{
				sceneIdx = i;
				break;
			}
		}
		if (sceneIdx < 0)
		{
			GD.PrintErr($"Structure scene not registered in spawner: {scenePath}");
			return;
		}

		// 4) Pack everything into one Variant array
		var data = new Godot.Collections.Array {
			sceneIdx,                 // which PackedScene to load
			navRegion.GetPath(),      // where to parent
			finalXform.Origin,        // position
			finalXform.Basis.GetRotationQuaternion() // rotation
		};

		Node spawnedNode = null;

		// 5) Only the server actually spawns & replicates
		// TODO: Will this work with single player? 
		if (Multiplayer.IsServer())
		{
			spawnedNode = _spawner.Spawn(data);
		}
		else
		{
			_spawner.RpcId(1, "Spawn", data);
			// client UI can optimistically update, or wait for the server's spawn callback
		}

		// 6) Update HUD if we have a local instance
		if (spawnedNode is StructureBase structure)
		{
			var me = PlayerManager.Instance.LocalPlayer;
			me.UpdateEnergy(structure.Energy);
			me.UpdateFunds(-structure.Cost);
			me.AddStructure(structure);

			// And if you need navmesh rebuild immediately on the server:
			if (Multiplayer.IsServer())
				_signals.EmitUpdateNavigationMap(navRegion);
		}
	}

	// private void PlaceStructure()
	// {
	// 	if (GlobalResources.Instance.IsHoveringUI)
	// 	{
	// 		CancelStructure();
	// 		return;
	// 	}

	// 	if (_placeholder == null || !_placeholder.ValidPlacement)
	// 		return;

	// 	// Raycast to find ground hit
	// 	var groundBody = _placeholder.GetHoveredMapBase(out Vector3 hitPos);
	// 	if (groundBody == null)
	// 	{
	// 		GD.PrintErr("PlaceStructure: Mouse not over map. Cancelling.");
	// 		CancelStructure();
	// 		return;
	// 	}

	// 	// Find nearest NavigationRegion3D
	// 	NavigationRegion3D navRegion = null;
	// 	Node walker = groundBody;
	// 	while (walker != null)
	// 	{
	// 		if (walker is NavigationRegion3D nr)
	// 		{
	// 			navRegion = nr;
	// 			break;
	// 		}
	// 		walker = walker.GetParent();
	// 	}
	// 	if (navRegion == null)
	// 	{
	// 		GD.PrintErr("PlaceStructure: No NavigationRegion3D parent found.");
	// 		CancelStructure();
	// 		return;
	// 	}

	// 	var finalXform = _placeholder.GlobalTransform;
	// 	CancelStructure();

	// 	// Delegate creation & bookkeeping to Player
	// 	var structure = _player.SpawnEntity<StructureBase>(
	// 		navRegion,
	// 		_models.Structures[Structure],
	// 		finalXform.Origin
	// 	);

	// 	// pack up: [sceneIndex, regionPath, pos, rotQuat]
	// 	int sceneIdx = _spawner.GetSpawnableSceneIndex(
	// 		_models.Structures[Structure].ResourcePath
	// 	);
	// 	var data = new Godot.Collections.Array {
	// 	sceneIdx,
	// 	navRegion.GetPath(),
	// 	finalXform.Origin,
	// 	finalXform.Basis.GetRotationQuaternion()
	// };

	// 	if (Multiplayer.IsServer())
	// 	{
	// 		// Host: spawn locally & replicate
	// 		_spawner.Spawn(data);
	// 	}
	// 	else
	// 	{
	// 		// Client: ask the server (peer 1) to spawn for everyone
	// 		_spawner.RpcId(1, "Spawn", data);
	// 	}

	// 	// update your HUD immediately…
	// 	var me = PlayerManager.Instance.LocalPlayer;
	// 	me.UpdateEnergy(structure.Energy);
	// 	me.UpdateFunds(-structure.Cost);

	// 	// // Capture final transform then cleanup placeholder
	// 	// var finalXform = _placeholder.GlobalTransform;
	// 	// CancelStructure();

	// 	// // Delegate creation & bookkeeping to Player
	// 	// var structure = _player.SpawnEntity<StructureBase>(
	// 	// 	navRegion,
	// 	// 	_models.Structures[Structure],
	// 	// 	finalXform.Origin
	// 	// );

	// 	// if (structure == null)
	// 	// 	return;

	// 	// structure.GlobalTransform = finalXform;
	// 	// _signals.EmitUpdateNavigationMap(navRegion);

	// 	// // Notify other systems
	// 	// Player player = PlayerManager.Instance.LocalPlayer;

	// 	// player.UpdateEnergy(structure.Energy);
	// 	// player.UpdateFunds(-structure.Cost);
	// 	// player.AddStructure(structure);
	// }

	private void CancelStructure()
	{
		GlobalResources.Instance.IsPlacingStructure = false;
		if (_placeholder == null) return;
		_placeholder.Area.AreaEntered -= _placeholder.OnAreaEntered;
		_placeholder.Area.AreaExited -= _placeholder.OnAreaExited;
		_placeholder.Overlaps.Clear();
		_placeholder.QueueFree();
		_placeholder = null;
	}

	private void RotatePlaceholder(float degrees)
	{
		var rot = _placeholder.RotationDegrees;
		rot.Y += degrees;
		_placeholder.RotationDegrees = rot;
	}

	private void OnBtnEnter()
	{
		var preview = _models.Structures[Structure].Instantiate<StructureBase>();
		_signals.EmitBuildOptionsBtnBtnHover(preview, null);
	}

	private void OnBtnExit()
	{
		_signals.EmitBuildOptionsBtnBtnHover(null, null);
	}
}
