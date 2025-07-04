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
	private PlayerManager _playerManager;
	private MultiplayerSpawner _multiplayerSpawner;
	private StructureFactory _structureFactory;

	public override void _Ready()
	{
		_multiplayerSpawner = GlobalResources.Instance.MultiplayerSpawner;
		_structureFactory = StructureFactory.Instance;

		// Grab the local Player from the PlayerManager
		_player = PlayerManager.Instance.LocalPlayer;
		if (_player == null)
			GD.PrintErr("[StructureBtn] No local player found!");

		_signals = Signals.Instance;
		_models = AssetServer.Instance.Models;
		_camera = GetViewport().GetCamera3D();
		_scene = GetTree().CurrentScene as Node3D;
		_playerManager = PlayerManager.Instance;

		Pressed += SelectStructure;
		MouseEntered += OnBtnEnter;
		MouseExited += OnBtnExit;

		if (Structure == StructureType.None)
			Utils.PrintErr($"Structure enum not set on {Name}");

		if (_scene == null)
			Utils.PrintErr("Current scene root is not a Node3D.");
	}

	private void SelectStructure()
	{
		// Cancel if already placing
		if (_placeholder != null)
		{
			CancelPlaceholder();
			return;
		}

		_placeholder = _structureFactory.BuildPlaceholder(Structure);

		if (_placeholder == null)
		{
			Utils.PrintErr($"Failed to create placeholder for structure: {Structure}");
			return;
		}

		_scene.AddChild(_placeholder);

		ReleaseFocus();
	}

	public override void _Input(InputEvent @event)
	{
		if (_placeholder == null)
			return;

		if (Input.IsActionJustPressed("mb_secondary"))
		{
			CancelPlaceholder();
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
			CancelPlaceholder();
			return;
		}

		if (_placeholder == null || !_placeholder.ValidPlacement)
			return;

		var navRegion = GetNavigationRegion();

		// Capture final transform then cleanup placeholder
		var finalXform = _placeholder.GlobalTransform;
		CancelPlaceholder();

		_signals.EmitUpdateNavigationMap(navRegion);

		if (!Multiplayer.IsServer())
			Rpc(nameof(ServerSpawnStructure), finalXform);
		else
			ServerSpawnStructure(finalXform);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void ServerSpawnStructure(Transform3D finalXform)
	{
		var structureScene = _models.Structures[Structure];
		StructureBase structure = structureScene.Instantiate<StructureBase>();
		structure.GlobalTransform = finalXform;

		var spawner = GlobalResources.Instance.MultiplayerSpawner;
		var parent = spawner.GetNode<Node3D>(spawner.SpawnPath);
		parent.AddChild(structure, true);

		var player = PlayerManager.Instance.LocalPlayer;
		player.AddStructure(structure);
	}

	private NavigationRegion3D GetNavigationRegion()
	{
		// Raycast to find ground hit
		var groundBody = _placeholder.GetHoveredMapBase(out Vector3 hitPos);
		if (groundBody == null)
		{
			GD.PrintErr("PlaceStructure: Mouse not over map. Cancelling.");
			CancelPlaceholder();
			return null;
		}

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
		if (navRegion == null)
		{
			GD.PrintErr("PlaceStructure: No NavigationRegion3D parent found.");
			CancelPlaceholder(); // TODO: Do I need this here?
			return null;
		}

		return navRegion;
	}

	private void CancelPlaceholder()
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
