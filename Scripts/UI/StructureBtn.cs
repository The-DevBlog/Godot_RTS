using Godot;
using MyEnums;

public partial class StructureBtn : Button
{
	[Export]
	public Structure Structure { get; set; }
	private Node3D _structurePlaceholder;
	private MyModels _models => AssetServer.Instance.Models;

	public override void _Ready()
	{
		if (Structure == Structure.None)
			Utils.PrintErr("Structure Enum is not set for " + Name);

		Pressed += OnButtonPressed;
	}

	public override void _Process(double delta)
	{
		if (_structurePlaceholder != null)
			UpdatePlaceholderPosition();
	}


	private void UpdatePlaceholderPosition()
	{
		// 1) Get the mouse position in viewport coordinates (Vector2)
		Camera3D camera = GetViewport().GetCamera3D();
		Vector2 mousePos = GetViewport().GetMousePosition();

		// 2) From the camera, compute a ray (origin + direction) at that screen point
		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
		Vector3 rayDirection = camera.ProjectRayNormal(mousePos);

		// 3) Intersect the ray against a horizontal plane (y = 0)
		Plane groundPlane = new Plane(Vector3.Up, 0.0f);
		// The plane intersection gives a point = rayOrigin + t * rayDirection
		var intersection = groundPlane.IntersectsSegment(
			rayOrigin,
			rayOrigin + rayDirection * 1000.0f // cast far out; adjust if needed
		);

		if (intersection.HasValue)
			_structurePlaceholder.GlobalPosition = intersection.Value;
	}

	private void OnButtonPressed()
	{
		if (Structure == Structure.None)
		{
			Utils.PrintErr("Structure is set to  none!");
			return;
		}

		PackedScene structureModel = _models.StructurePlaceholders[Structure];
		// PackedScene structureModel = _models.Structures[Structure];
		Node3D structure = structureModel.Instantiate() as Node3D;
		if (structure == null)
		{
			Utils.PrintErr("Failed to instantiate structure for " + Structure);
			return;
		}

		Node3D currentScene = GetTree().CurrentScene as Node3D;
		if (currentScene == null)
		{
			Utils.PrintErr("Current scene is not a Node3D.");
			return;
		}

		_structurePlaceholder = structure;
		currentScene.AddChild(_structurePlaceholder);
	}
}
