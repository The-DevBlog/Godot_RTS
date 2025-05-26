using Godot;

public partial class MouseManagerCs : Node3D
{
	private Camera3D camera;

	private bool _dragActive = false;

	public override void _Ready()
	{
		camera = GetNode<Camera3D>("../Camera/CameraPosition/CameraRotationX/CameraZoomPivot/Camera3D");
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton)
			return;

		if (Input.IsActionJustPressed("mb_primary"))
		{
			var dragStart = GetMouseWorldPosition(camera);
			if (dragStart != Vector3.Zero)
			{
				_dragActive = true;
				GD.Print("CS Drag start: ", dragStart);
			}
		}

		if (Input.IsActionJustReleased("mb_primary"))
		{
			var dragEnd = GetMouseWorldPosition(camera);
			if (dragEnd != Vector3.Zero)
			{
				_dragActive = false;
				GD.Print("CS Drag end: ", dragEnd);
			}
		}
	}

	private Vector3 GetMouseWorldPosition(Camera3D cam)
	{
		var mousePos = GetViewport().GetMousePosition();
		var from = cam.ProjectRayOrigin(mousePos);
		var to = from + cam.ProjectRayNormal(mousePos) * 1000;

		var spaceState = cam.GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithAreas = false;
		query.CollideWithBodies = true;

		var result = spaceState.IntersectRay(query);
		if (result.Count > 0)
			return (Vector3)result["position"];

		return Vector3.Zero;
	}
}
