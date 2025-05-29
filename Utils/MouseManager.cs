using System.Collections.Generic;
using Godot;

public partial class MouseManager : Control
{
	public static MouseManager Instance { get; private set; }
	private const float MIN_DRAG_DIST = 10f;
	private bool _mouseDown;
	private Camera3D _camera;
	private bool _dragActive = false;
	private Vector2 _dragStart = Vector2.Zero;
	private Vector2 _dragEnd = Vector2.Zero;
	private HashSet<Unit> _prevSelectedUnits = new HashSet<Unit>();

	public override void _Ready()
	{
		Instance = this;
		_camera = GetViewport().GetCamera3D();
	}

	public override void _Process(double delta)
	{
		if (_mouseDown)
			// SetDragEndPosition();

			// only if we have a “real” drag do we update selection:
			if (_dragActive && (_dragEnd - _dragStart).Length() > MIN_DRAG_DIST)
			{
				var selected = GetUnitsInSelection();
				MarkSelectedUnits(selected);
			}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
		{
			if (mb.Pressed)
			{
				_dragStart = mb.Position;
				_dragEnd = mb.Position;
				_mouseDown = true;
				_dragActive = false;           // not a drag… yet
			}
			else
			{
				if (!_dragActive)
				{
					SetTargetPosition(mb.Position);
				}

				_mouseDown = false;
				_dragActive = false;
				QueueRedraw();
			}
		}
		else if (_mouseDown && @event is InputEventMouseMotion mm)
		{
			// once we pass the threshold, flip on real-drag mode:
			if (!_dragActive && (mm.Position - _dragStart).Length() > MIN_DRAG_DIST)
				_dragActive = true;

			_dragEnd = mm.Position;
			QueueRedraw();
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

	private void SetTargetPosition(Vector2 position)
	{
		var cam = GetViewport().GetCamera3D();
		Vector2 mousePos = position; // or GetViewport().GetMousePosition()

		// build a ray
		Vector3 from = cam.ProjectRayOrigin(mousePos);
		Vector3 to = from + cam.ProjectRayNormal(mousePos) * 1000f;

		// get the world directly from the camera
		var spaceState = cam.GetWorld3D().DirectSpaceState;

		var rayParams = new PhysicsRayQueryParameters3D
		{
			From = from,
			To = to,
		};

		var result = spaceState.IntersectRay(rayParams);
		if (result.TryGetValue("position", out var hitPosition))
			Signals.Instance.EmitSignal(nameof(Signals.Instance.SetTargetPosition), hitPosition.AsVector3());
	}

	// Updates the drag state based on mouse input.
	// Updates the start and end positions of the drag rectangle while dragging.
	private void UpdateDragState(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseEvent)
			return;

		if (mouseEvent.IsPressed() && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			_dragStart = GetViewport().GetMousePosition();
			_dragEnd = _dragStart;
			_dragActive = true;
		}

		if (!mouseEvent.IsPressed() && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			_dragActive = false;
			QueueRedraw();
		}
	}

	// Returns a list of units that are within the selection rectangle.
	private List<Unit> GetUnitsInSelection()
	{
		var selectRect = new Rect2(_dragStart, _dragEnd - _dragStart).Abs();

		var picked = new List<Unit>();
		foreach (Unit unit in GetTree().GetNodesInGroup("units"))
		{
			var screenPoint = _camera.UnprojectPosition(unit.GlobalPosition);

			if (selectRect.HasPoint(screenPoint))
				picked.Add(unit);
		}

		return picked;
	}

	// Marks units as selected or unselected.
	// The _prevSelectedUnits set is used to track the previous state of selection. (more efficient than looping through all units)
	private void MarkSelectedUnits(List<Unit> units)
	{
		var selectedUnits = new HashSet<Unit>(units);

		foreach (Unit unit in _prevSelectedUnits)
		{
			if (!selectedUnits.Contains(unit))
				unit.Selected = false;
		}

		foreach (Unit unit in selectedUnits)
		{
			if (!_prevSelectedUnits.Contains(unit))
				unit.Selected = true;
		}

		_prevSelectedUnits = selectedUnits;
	}
}
