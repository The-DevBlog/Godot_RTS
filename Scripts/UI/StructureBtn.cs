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

	public override void _Ready()
	{
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

		// Raycast to find ground hit
		var groundBody = _placeholder.GetHoveredMapBase(out Vector3 hitPos);
		if (groundBody == null)
		{
			GD.PrintErr("PlaceStructure: Mouse not over map. Cancelling.");
			CancelStructure();
			return;
		}

		// Find nearest NavigationRegion3D
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
			CancelStructure();
			return;
		}

		// Capture final transform then cleanup placeholder
		var finalXform = _placeholder.GlobalTransform;
		CancelStructure();

		// Delegate creation & bookkeeping to Player
		var structure = _player.SpawnEntity<StructureBase>(
			navRegion,
			_models.Structures[Structure],
			finalXform.Origin
		);

		if (structure == null)
			return;

		structure.GlobalTransform = finalXform;
		_signals.EmitUpdateNavigationMap(navRegion);

		// Notify other systems
		Player player = PlayerManager.Instance.LocalPlayer;

		// player.UpdateEnergy(25);
		// player.UpdateFunds(-500);

		player.UpdateEnergy(structure.Energy);
		player.UpdateFunds(-structure.Cost);
		player.AddStructure(structure);
	}

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
