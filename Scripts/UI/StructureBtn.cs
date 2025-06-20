using System.Collections.Generic;
using Godot;
using MyEnums;

public partial class StructureBtn : Button
{
	[Export] public StructureType Structure { get; set; }
	private GlobalResources _globalResources;
	private SceneResources _sceneResources;
	private Signals _signals;
	private StructureBase _structure;
	private MyModels _models;
	private Camera3D _camera;
	private bool _validPlacement => _overlaps.Count == 0;
	private readonly HashSet<Area3D> _overlaps = new();
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

	public override void _Process(double delta)
	{
		GD.Print("is placing strcuture: " + _globalResources.IsPlacingStructure);
		if (_structure != null)
		{
			GetHoveredMapBase(out Vector3 hitPos);
			_structure.GlobalPosition = hitPos;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_structure == null)
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
				RotatePlaceholder(45.0f);
			else if (mb.ButtonIndex == MouseButton.WheelDown)
				RotatePlaceholder(-45.0f);
		}
	}

	private void RotatePlaceholder(float degrees)
	{
		var newRotation = _structure.RotationDegrees;
		newRotation.Y += degrees;

		_structure.RotationDegrees = newRotation;
	}

	private void CancelStructure()
	{
		_globalResources.IsPlacingStructure = false;
		_scene.RemoveChild(_structure);

		_structure.QueueFree();
		_structure = null;

		_overlaps.Clear();

		GlobalResources.Instance.IsPlacingStructure = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void OnStructureSelect()
	{
		if (_structure != null)
		{
			CancelStructure();
			return;
		}

		// deselect all units
		_signals.EmitSignal(nameof(_signals.DeselectAllUnits));

		PackedScene structureModel = _models.StructurePlaceholders[Structure];
		StructureBase structure = structureModel.Instantiate() as StructureBase;
		if (structure == null)
		{
			Utils.PrintErr("Failed to instantiate structure for " + Structure);
			return;
		}

		// check if max structure count reached
		bool maxStructureCount = _globalResources.MaxStructureCountReached(Structure);
		if (maxStructureCount)
			return;

		// check if you have enough funds
		bool enoughFunds = _sceneResources.Funds >= structure.Cost;
		if (!enoughFunds)
		{
			GD.Print("Not enough funds!");
			return;
		}

		_structure = structure;

		GlobalResources.Instance.IsPlacingStructure = true;
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		_scene.AddChild(_structure);

		// connect signals for area overlap detection
		_structure.Area.AreaEntered += OnAreaEntered;
		_structure.Area.AreaExited += OnAreaExited;

		this.ReleaseFocus();
	}

	private void PlaceStructure()
	{
		if (_structure == null || !_validPlacement || _globalResources.IsHoveringUI)
			return;

		// 1) Ray‐cast under the mouse and get (groundBody, hitPos)
		StaticBody3D groundBody = GetHoveredMapBase(out Vector3 hitPos);
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

		// 3) Cache placeholder’s final world‐transform, then free it
		Vector3 finalPos = _structure.GlobalPosition;
		Basis finalBasis = _structure.GlobalTransform.Basis;
		_scene.RemoveChild(_structure);
		_structure.QueueFree();
		_structure = null;
		_globalResources.IsPlacingStructure = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;

		// 4) Instantiate the real structure under navRegion
		PackedScene realScene = _models.Structures[Structure];
		Node3D structure = realScene.Instantiate() as Node3D;
		if (structure == null)
		{
			Utils.PrintErr("Failed to instantiate structure for " + Structure);
			return;
		}

		navRegion.AddChild(structure);

		// 5) Apply the placeholder’s world transform (GlobalPosition + rotation)
		structure.GlobalPosition = finalPos;
		structure.GlobalTransform = new Transform3D(finalBasis, finalPos);

		// 6) Tell listeners to rebuild navmesh for only that one region
		_signals.EmitUpdateNavigationMap(navRegion);

		var structureBase = structure as StructureBase;
		if (structureBase == null)
			Utils.PrintErr("StructureBase class is not assigned to structure: " + Structure);

		_signals.EmitUpdateEnergy(structureBase.Energy);
		_signals.EmitUpdateFunds(-structureBase.Cost);
		_signals.EmitAddStructure(Structure);
	}

	// Casts a ray under the mouse, returns the first StaticBody3D in "MapBase" and the hit position.
	private StaticBody3D GetHoveredMapBase(out Vector3 hitPosition)
	{
		hitPosition = Vector3.Zero;

		// Build the ray under the mouse
		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector3 rayOrigin = _camera.ProjectRayOrigin(mousePos);
		Vector3 rayDirection = _camera.ProjectRayNormal(mousePos);
		Vector3 rayEnd = rayOrigin + rayDirection * 1000.0f;

		var spaceState = _camera.GetWorld3D().DirectSpaceState;
		var query = new PhysicsRayQueryParameters3D
		{
			From = rayOrigin,
			To = rayEnd,
			CollisionMask = 2,
		};

		var result = spaceState.IntersectRay(query);
		if (result.Count == 0)
			return null;

		// Extract the collider and check if it’s a StaticBody3D in MapBase
		CollisionObject3D colObj = (CollisionObject3D)result["collider"];
		if (colObj == null)
			return null;

		StaticBody3D bodyHit = colObj as StaticBody3D;
		if (bodyHit == null || !bodyHit.IsInGroup(Group.MapBase.ToString()))
			return null;

		hitPosition = (Vector3)result["position"];
		return bodyHit;
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

	private void OnAreaEntered(Area3D other)
	{
		if (other == _structure.Area) return;   // ignore self-entering
		_overlaps.Add(other);
		// optional: update visuals here
	}

	private void OnAreaExited(Area3D other)
	{
		_overlaps.Remove(other);
		// optional: update visuals here
	}
}
