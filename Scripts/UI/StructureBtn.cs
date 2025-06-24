using Godot;
using MyEnums;

public partial class StructureBtn : Button
{
	[Export] public StructureType Structure { get; set; }
	private GlobalResources _globalResources;
	private SceneResources _sceneResources;
	private Signals _signals;
	private StructureBasePlaceholder _placeholder;
	private StructureBase _structure;
	private MyModels _models;
	private Camera3D _camera;
	private Node3D _scene;
	public override void _Ready()
	{
		_globalResources = GlobalResources.Instance;
		_sceneResources = SceneResources.Instance;
		_signals = Signals.Instance;
		_models = AssetServer.Instance.Models;
		_camera = GetViewport().GetCamera3D();
		_scene = GetTree().CurrentScene as Node3D;

		Pressed += OnStructureSelect;
		MouseEntered += OnBtnEnter;
		MouseExited += OnBtnExit;

		if (Structure == StructureType.None)
			Utils.PrintErr("Structure Enum is not set for " + Name);

		if (_scene == null)
			Utils.PrintErr("Current scene is not a Node3D.");
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
			PlaceStructure();

		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			if (mb.ButtonIndex == MouseButton.WheelUp)
				RotatePlaceholder(90.0f);
			else if (mb.ButtonIndex == MouseButton.WheelDown)
				RotatePlaceholder(-90.0f);
		}
	}

	private void RotatePlaceholder(float degrees)
	{
		var newRotation = _placeholder.RotationDegrees;
		newRotation.Y += degrees;

		_placeholder.RotationDegrees = newRotation;
	}

	private void CancelStructure()
	{
		_globalResources.IsPlacingStructure = false;

		if (_placeholder == null) return;

		_placeholder.Area.AreaEntered -= _placeholder.OnAreaEntered;
		_placeholder.Area.AreaExited -= _placeholder.OnAreaExited;
		_placeholder.QueueFree();
		_placeholder.Overlaps.Clear();
		_placeholder = null;
	}

	private void OnStructureSelect()
	{
		if (_placeholder != null)
		{
			CancelStructure();
			return;
		}

		// deselect all units
		_signals.EmitSignal(nameof(_signals.DeselectAllUnits));

		var structureScene = _models.Structures[Structure];
		StructureBase structureInstance = structureScene.Instantiate<StructureBase>();

		if (structureInstance == null)
		{
			Utils.PrintErr("Failed to instantiate actual structure for " + Structure);
			return;
		}

		bool maxStructureCount = _sceneResources.MaxStructureCountReached(Structure);
		if (maxStructureCount)
			return;

		bool enoughFunds = _sceneResources.Funds >= structureInstance.Cost;
		if (!enoughFunds)
		{
			GD.Print("Not enough funds!");
			return;
		}

		_placeholder = _models.StructurePlaceholders[Structure].Instantiate<StructureBasePlaceholder>();

		GlobalResources.Instance.IsPlacingStructure = true;
		_scene.AddChild(_placeholder);

		this.ReleaseFocus();
	}

	private void PlaceStructure()
	{
		if (_globalResources.IsHoveringUI)
		{
			CancelStructure();
			return;
		}

		if (_placeholder == null || !_placeholder.ValidPlacement)
			return;


		// 1) Ray‐cast under the mouse and get (groundBody, hitPos)
		StaticBody3D groundBody = _placeholder.GetHoveredMapBase(out Vector3 hitPos);
		if (groundBody == null)
		{
			GD.PrintErr("PlaceStructure: Mouse not over MapBase. Cancelling placement.");
			return;
		}

		// 2) Find the parent NavigationRegion3D of that StaticBody3D
		NavigationRegion3D navRegion = null;
		Node current = groundBody;
		while (current != null)
		{
			if (current is NavigationRegion3D nr)
			{
				navRegion = nr;
				break;
			}

			current = current.GetParent();
		}

		if (navRegion == null)
		{
			GD.PrintErr($"PlaceStructure: '{groundBody.Name}' has no parent NavigationRegion3D. Cancelling.");
			return;
		}

		// 3) finish and remove placeholder
		var finalTransform = _placeholder.GlobalTransform;
		_placeholder.Area.AreaEntered -= _placeholder.OnAreaEntered;
		_placeholder.Area.AreaExited -= _placeholder.OnAreaExited;
		_placeholder.QueueFree();
		_placeholder = null;
		GlobalResources.Instance.IsPlacingStructure = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;

		// 4) Instantiate the real structure under navRegion
		_structure = _models.Structures[Structure].Instantiate<StructureBase>();
		navRegion.AddChild(_structure);

		// 5) Apply the placeholder’s world transform (GlobalPosition + rotation)
		_structure.GlobalTransform = finalTransform;

		// 6) Tell listeners to rebuild navmesh for only that one region
		_signals.EmitUpdateNavigationMap(navRegion);
		_signals.EmitUpdateEnergy(_structure.Energy);
		_signals.EmitUpdateFunds(-_structure.Cost);
		_signals.EmitAddStructure(Structure);
	}

	private void OnBtnEnter()
	{
		var packed = _models.Structures[Structure];
		var structure = packed.Instantiate<StructureBase>();

		_signals.EmitBuildOptionsBtnBtnHover(structure, null);
	}

	private void OnBtnExit()
	{
		_signals.EmitBuildOptionsBtnBtnHover(null, null);
	}
}
