using Godot;
using MyEnums;

public partial class StructureBtn : Button
{
	[Export] public Structure Structure { get; set; }
	private Resources _resources;
	private Signals _signals;
	private Node3D _structurePlaceholder;
	private MyModels _models;
	private Camera3D _camera;
	private Node3D _scene;

	public override void _Ready()
	{
		_resources = Resources.Instance;
		_signals = Signals.Instance;
		_models = AssetServer.Instance.Models;
		_camera = GetViewport().GetCamera3D();
		_scene = GetTree().CurrentScene as Node3D;

		Pressed += OnStructureSelect;

		if (Structure == Structure.None)
			Utils.PrintErr("Structure Enum is not set for " + Name);

		if (_scene == null)
			Utils.PrintErr("Current scene is not a Node3D.");
	}

	public override void _Process(double delta)
	{
		if (_structurePlaceholder != null)
		{
			GetHoveredMapBase(out Vector3 hitPos);
			_structurePlaceholder.GlobalPosition = hitPos;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_structurePlaceholder == null)
			return;

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
		var newRotation = _structurePlaceholder.RotationDegrees;
		newRotation.Y += degrees;

		_structurePlaceholder.RotationDegrees = newRotation;
	}

	private void OnStructureSelect()
	{
		// deselect all units
		_signals.EmitSignal(nameof(_signals.DeselectAllUnits));

		PackedScene structureModel = _models.StructurePlaceholders[Structure];
		Node3D structure = structureModel.Instantiate() as Node3D;
		if (structure == null)
		{
			Utils.PrintErr("Failed to instantiate structure for " + Structure);
			return;
		}

		_structurePlaceholder = structure;
		Resources.Instance.IsPlacingStructure = true;
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		_scene.AddChild(_structurePlaceholder);

		this.ReleaseFocus();
	}

	private void PlaceStructure()
	{
		// bool enoughFunds = _resources.Funds >= 
		if (_structurePlaceholder == null)
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
		Vector3 finalPos = _structurePlaceholder.GlobalPosition;
		Basis finalBasis = _structurePlaceholder.GlobalTransform.Basis;
		_scene.RemoveChild(_structurePlaceholder);
		_structurePlaceholder.QueueFree();
		_structurePlaceholder = null;
		Resources.Instance.IsPlacingStructure = false;
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

		GD.Print("Structure placed: " + Structure);
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
}
