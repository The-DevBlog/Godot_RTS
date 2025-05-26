using Godot;

public partial class MouseManager : Control
{
	private Camera3D _camera;
	private bool _dragActive = false;
	private Vector3 _dragStart = Vector3.Zero;
	private Vector3 _dragEnd = Vector3.Zero;

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("../Camera/CameraPosition/CameraRotationX/CameraZoomPivot/Camera3D");

		// Size = GetViewportRect().Size;
		// SetAnchorsPreset(LayoutPreset.FullRect);
	}

	public override void _Process(double delta)
	{
		SetDragActive();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton)
			return;

		if (Input.IsActionJustPressed("mb_primary"))
		{
			_dragStart = GetMouseWorldPosition(_camera);
			_dragActive = true;
		}

		if (Input.IsActionJustReleased("mb_primary"))
		{
			_dragActive = false;
			QueueRedraw(); // Triggers redraw to erase the rectangle
		}
	}

	public override void _Draw()
	{
		if (!_dragActive)
			return;

		Vector2 viewportDragStart = _camera.UnprojectPosition(_dragStart);
		Vector2 viewportDragEnd = _camera.UnprojectPosition(_dragEnd);

		if (_dragActive)
		{
			var rect = new Rect2(viewportDragStart, viewportDragEnd - viewportDragStart).Abs();
			DrawRect(rect, new Color(0.2f, 0.6f, 1.0f, 0.3f), filled: false);
			DrawRect(rect, new Color(0.2f, 0.6f, 1.0f), filled: false, width: 2);
		}
	}

	private void SetDragActive()
	{
		if (_dragActive && Input.IsActionPressed("mb_primary"))
		{
			_dragEnd = GetMouseWorldPosition(_camera);
			QueueRedraw();
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
