using System.Collections.Generic;
using Godot;

public partial class MouseManager : Control
{
	public static MouseManager Instance { get; private set; }
	public bool DragActive { get; set; } = false;
	private Camera3D _camera;
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
		if (DragActive)
		{
			SetDragEndPosition();

			var selectedUnits = GetUnitsInSelection();
			MarkSelectedUnits(selectedUnits);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		UpdateDragState(@event);
	}

	public override void _Draw()
	{
		if (!DragActive)
			return;

		var rect = new Rect2(_dragStart, _dragEnd - _dragStart).Abs();
		DrawRect(rect, new Color(0.2f, 0.6f, 1.0f, 0.3f), filled: true);
		DrawRect(rect, new Color(0.2f, 0.6f, 1.0f), filled: false, width: 2);
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
			DragActive = true;
		}

		if (!mouseEvent.IsPressed() && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			DragActive = false;
			QueueRedraw();
		}
	}

	// Updates the drag end position if the primary mouse button is pressed, then queues redraw
	private void SetDragEndPosition()
	{
		if (Input.IsActionPressed("mb_primary"))
		{
			_dragEnd = GetViewport().GetMousePosition();
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
