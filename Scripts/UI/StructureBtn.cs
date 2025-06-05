using Godot;
using MyEnums;

public partial class StructureBtn : Button
{
	[Export]
	public Structure Structure { get; set; }
	private Node3D _structurePlaceholder;
	private MyModels _models;
	private Camera3D _camera;
	private Node3D _scene;

	public override void _Ready()
	{
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
			UpdatePlaceholderPosition();
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


	private void UpdatePlaceholderPosition()
	{
		_structurePlaceholder.GlobalPosition = GetWorldPosition();
	}

	private void RotatePlaceholder(float degrees)
	{
		var newRotation = _structurePlaceholder.RotationDegrees;
		newRotation.Y += degrees;

		_structurePlaceholder.RotationDegrees = newRotation;
	}

	private void OnStructureSelect()
	{
		PackedScene structureModel = _models.StructurePlaceholders[Structure];
		Node3D structure = structureModel.Instantiate() as Node3D;
		if (structure == null)
		{
			Utils.PrintErr("Failed to instantiate structure for " + Structure);
			return;
		}

		_structurePlaceholder = structure;
		Resources.Instance.IsPlacingStructure = true;
		_scene.AddChild(_structurePlaceholder);

		this.ReleaseFocus();
	}

	private void PlaceStructure()
	{
		if (_structurePlaceholder == null)
			return;

		PackedScene structureModel = _models.Structures[Structure];
		Node3D structure = structureModel.Instantiate() as Node3D;
		if (structure == null)
		{
			Utils.PrintErr("Failed to instantiate structure for " + Structure);
			return;
		}

		_scene.AddChild(structure);

		// Update position to match current mouse position
		Vector3 position = GetWorldPosition();
		structure.GlobalPosition = position;
		structure.Rotation = _structurePlaceholder.Rotation;

		_scene.RemoveChild(_structurePlaceholder);
		_structurePlaceholder = null;
		Resources.Instance.IsPlacingStructure = false;
	}

	private Vector3 GetWorldPosition()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();

		// From the camera, compute a ray (origin + direction) at that screen point
		Vector3 rayOrigin = _camera.ProjectRayOrigin(mousePos);
		Vector3 rayDirection = _camera.ProjectRayNormal(mousePos);

		// Intersect the ray against a horizontal plane (y = 0)
		Plane groundPlane = new Plane(Vector3.Up, 0.0f);
		Vector3? intersection = groundPlane.IntersectsSegment(
			rayOrigin,
			rayOrigin + rayDirection * 1000.0f
		);

		return intersection.Value;
	}
}
