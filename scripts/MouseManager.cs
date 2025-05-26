using Godot;

public partial class MouseManager : Control
{
	private Camera3D _camera;
	private bool _dragActive = false;
	private Vector2 _dragStart = Vector2.Zero;
	private Vector2 _dragEnd = Vector2.Zero;

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("../Camera/CameraPosition/CameraRotationX/CameraZoomPivot/Camera3D");
	}

	public override void _Process(double delta)
	{
		CountSelectedUnits();
		SetDragActive();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton)
			return;

		if (Input.IsActionJustPressed("mb_primary"))
		{
			_dragStart = GetViewport().GetMousePosition();
			_dragEnd = _dragStart; // Initialize end point to start point
			_dragActive = true;
		}

		if (Input.IsActionJustReleased("mb_primary"))
		{
			_dragActive = false;
			QueueRedraw(); // Triggers redraw to erase the rectangle

			// Selection logic
			var rect = new Rect2(_dragStart, _dragEnd - _dragStart).Abs();

			foreach (Unit unit in GetTree().GetNodesInGroup("units"))
			{
				Vector3 unitWorldPos = unit.GlobalTransform.Origin;
				Vector2 unitScreenPos = _camera.UnprojectPosition(unitWorldPos);

				// The unit is within the selection box
				if (rect.HasPoint(unitScreenPos))
					unit.Selected = true;
				else
				{
				}
			}
		}
	}

	public override void _Draw()
	{
		if (!_dragActive)
			return;

		if (_dragActive)
		{
			var rect = new Rect2(_dragStart, _dragEnd - _dragStart).Abs();
			DrawRect(rect, new Color(0.2f, 0.6f, 1.0f, 0.3f), filled: false);
			DrawRect(rect, new Color(0.2f, 0.6f, 1.0f), filled: false, width: 2);
		}
	}

	private void CountSelectedUnits()
	{
		int count = 0;
		foreach (Unit unit in GetTree().GetNodesInGroup("units"))
		{
			if (unit.Selected)
				count++;
		}

		GD.Print($"Selected units count: {count}");
	}

	private void SetDragActive()
	{
		if (_dragActive && Input.IsActionPressed("mb_primary"))
		{
			_dragEnd = GetViewport().GetMousePosition();
			QueueRedraw();
		}
	}
}
