using System.Collections.Generic;
using Godot;

public partial class Mouse : Control
{
	private Camera3D _camera;
	private bool _dragActive = false;
	private Vector2 _dragStart = Vector2.Zero;
	private Vector2 _dragEnd = Vector2.Zero;
	private HashSet<Unit> _prevSelectedUnits = new HashSet<Unit>();

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("../Camera/CameraPosition/CameraRotationX/CameraZoomPivot/Camera3D");
	}

	public override void _Process(double delta)
	{
		SetDragActive();

		if (_dragActive)
		{
			var selectedUnits = GetUnitsInSelection();
			MarkSelectedUnits(selectedUnits);
		}
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton mouseEvent)
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

	public override void _Draw()
	{
		if (!_dragActive)
			return;

		var rect = new Rect2(_dragStart, _dragEnd - _dragStart).Abs();
		DrawRect(rect, new Color(0.2f, 0.6f, 1.0f, 0.3f), filled: true);
		DrawRect(rect, new Color(0.2f, 0.6f, 1.0f), filled: false, width: 2);
	}

	private List<Unit> GetUnitsInSelection()
	{
		var selRect = new Rect2(_dragStart, _dragEnd - _dragStart).Abs();

		var picked = new List<Unit>();
		foreach (Unit unit in GetTree().GetNodesInGroup("units"))
		{
			var worldPos = unit.GlobalTransform.Origin;
			if (_camera.IsPositionBehind(worldPos))
				continue;

			var screenPoint = _camera.UnprojectPosition(worldPos);

			if (selRect.HasPoint(screenPoint))
				picked.Add(unit);
		}

		return picked;
	}

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

	private void SetDragActive()
	{
		if (_dragActive && Input.IsActionPressed("mb_primary"))
		{
			_dragEnd = GetViewport().GetMousePosition();
			QueueRedraw();
		}
	}
}
