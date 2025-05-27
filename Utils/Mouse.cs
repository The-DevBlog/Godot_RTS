using Godot;

public partial class Mouse : Control
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
		SetDragActive();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton mouseEvent)
			return;

		if (mouseEvent.IssPressed() && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			_dragStart = GetViewport().GetMousePosition();
			_dragEnd = _dragStart;
			_dragActive = true;
		}

		if (!mouseEvent.IsPressed() && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			_dragActive = false;
			QueueRedraw();

			// Project drag corners onto ground plane
			Vector2 topLeft = new Vector2(Mathf.Min(_dragStart.X, _dragEnd.X), Mathf.Min(_dragStart.Y, _dragEnd.Y));
			Vector2 bottomRight = new Vector2(Mathf.Max(_dragStart.X, _dragEnd.X), Mathf.Max(_dragStart.Y, _dragEnd.Y));
			Vector2 topRight = new Vector2(bottomRight.X, topLeft.Y);
			Vector2 bottomLeft = new Vector2(topLeft.X, bottomRight.Y);

			Vector3 p1 = GetGroundPoint(topLeft);
			Vector3 p2 = GetGroundPoint(topRight);
			Vector3 p3 = GetGroundPoint(bottomRight);
			Vector3 p4 = GetGroundPoint(bottomLeft);

			foreach (Unit unit in GetTree().GetNodesInGroup("units"))
			{
				Vector3 unitPos = unit.GlobalTransform.Origin;
				unitPos.Y = 0; // Project onto ground plane

				bool selected = PointInQuad(unitPos, p1, p2, p3, p4);
				unit.Selected = selected;
			}
		}
	}

	public override void _Draw()
	{
		if (!_dragActive)
			return;

		var rect = new Rect2(_dragStart, _dragEnd - _dragStart).Abs();
		DrawRect(rect, new Color(0.2f, 0.6f, 1.0f, 0.3f), filled: true);
		DrawRect(rect, new Color(0.2f, 0.6f, 1.0f), filled: false, width: 2);
	}

	private void SetDragActive()
	{
		if (_dragActive && Input.IsActionPressed("mb_primary"))
		{
			_dragEnd = GetViewport().GetMousePosition();
			QueueRedraw();
		}
	}

	private Vector3 GetGroundPoint(Vector2 screenPos)
	{
		Vector3 origin = _camera.ProjectRayOrigin(screenPos);
		Vector3 direction = _camera.ProjectRayNormal(screenPos);

		// Ray-plane intersection with Y=0 ground plane
		if (Mathf.IsZeroApprox(direction.Y))
			return origin;

		float t = -origin.Y / direction.Y;
		return origin + direction * t;
	}

	private bool PointInQuad(Vector3 p, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		return IsPointInTriangle(p, a, b, c) || IsPointInTriangle(p, a, c, d);
	}

	private bool IsPointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
	{
		Vector3 v0 = c - a;
		Vector3 v1 = b - a;
		Vector3 v2 = p - a;

		float dot00 = v0.Dot(v0);
		float dot01 = v0.Dot(v1);
		float dot02 = v0.Dot(v2);
		float dot11 = v1.Dot(v1);
		float dot12 = v1.Dot(v2);

		float denom = dot00 * dot11 - dot01 * dot01;
		if (Mathf.IsZeroApprox(denom)) return false;

		float u = (dot11 * dot02 - dot01 * dot12) / denom;
		float v = (dot00 * dot12 - dot01 * dot02) / denom;

		return (u >= 0) && (v >= 0) && (u + v <= 1);
	}
}
