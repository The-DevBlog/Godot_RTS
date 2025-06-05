using System.Collections.Generic;
using System.Linq;

using Godot;
using MyEnums;

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
		HandleMouseInput();
	}

	public override void _Draw()
	{
		if (!_dragActive)
			return;

		var rect = new Rect2(_dragStart, _dragEnd - _dragStart).Abs();
		DrawRect(rect, new Color(0.2f, 0.6f, 1.0f, 0.3f), filled: true);
		DrawRect(rect, new Color(0.2f, 0.6f, 1.0f), filled: false, width: 2);
	}

	private void HandleMouseInput()
	{
		// 1) Pressed right now? begin potential drag:
		if (Input.IsActionJustPressed("mb_primary"))
		{
			_dragStart = GetViewport().GetMousePosition();
			_dragEnd = _dragStart;
			_mouseDown = true;
			_dragActive = false;   // we haven’t moved far enough yet
			QueueRedraw();
		}
		// 2) Released right now? end drag or treat as click:
		else if (Input.IsActionJustReleased("mb_primary"))
		{
			if (!_dragActive)
			{
				// it never moved beyond the threshold → a click!
				SetTargetPosition(GetViewport().GetMousePosition());
			}

			_mouseDown = false;
			_dragActive = false;
			QueueRedraw();
		}
		// 3) Still holding? update drag distance & maybe select:
		else if (_mouseDown && Input.IsActionPressed("mb_primary"))
		{
			Vector2 mousePos = GetViewport().GetMousePosition();

			// If we move past the threshold, flip into real drag mode:
			if (!_dragActive && (mousePos - _dragStart).Length() > MIN_DRAG_DIST)
				_dragActive = true;

			_dragEnd = mousePos;
			QueueRedraw();

			// If it’s a real drag, update the marquee‐select:
			if (_dragActive)
			{
				var selectedUnits = GetUnitsInSelection();
				MarkSelectedUnits(selectedUnits);
			}
		}
	}

	private void SetTargetPosition(Vector2 mousePos)
	{

		if (_camera == null)
		{
			Utils.PrintErr("Camera3D not found.");
			return;
		}

		var cam = _camera;
		Vector3 from = cam.ProjectRayOrigin(mousePos);
		Vector3 to = from + cam.ProjectRayNormal(mousePos) * 1000f;

		var rayParams = new PhysicsRayQueryParameters3D { From = from, To = to };
		var result = cam.GetWorld3D().DirectSpaceState.IntersectRay(rayParams);
		if (!result.TryGetValue("position", out var hitVar))
			return;

		Vector3 center = hitVar.AsVector3();

		// collect selected units
		var selectedUnits = GetTree()
			.GetNodesInGroup(Group.Units.ToString())
			.OfType<Unit>()
			.Where(u => u.Selected)
			.ToArray();

		int count = selectedUnits.Length;
		if (count == 0) return;

		// define the square “formation” size
		float sideLength = 4.0f;   // total width/depth of the area
		float half = sideLength / 2f;

		// compute a grid of rows × cols that fits all units
		int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
		int rows = Mathf.CeilToInt((float)count / cols);

		// compute cell size so they’re evenly spaced (with half-cell margin)
		float cellW = sideLength / cols;
		float cellH = sideLength / rows;

		// loop and offset each unit into its cell’s center
		for (int i = 0; i < count; i++)
		{
			var unit = selectedUnits[i];
			var colShapeNode = unit.GetNode<CollisionShape3D>("CollisionShape3D");
			if (colShapeNode == null)
			{
				Utils.PrintErr("Could not get CollisionShape3D!");
				continue;
			}

			if (!(colShapeNode.Shape is CylinderShape3D cyl))
			{
				Utils.PrintErr("Units shape is not a cylinder!");
				continue;
			}

			// 1) Compute per-unit spacing: diameter + extra padding
			float diameter = cyl.Radius * 2f;
			float padding = 0.5f;        // tweak this if you want more gap
			float unitSpacing = diameter + padding;

			// Build a “base” offset so the grid is centered on (0,0)
			Vector3 baseOffset = new Vector3(
				-((cols - 1) * unitSpacing) / 2f,
				 0f,
				-((rows - 1) * unitSpacing) / 2f
			);

			// Cell indices
			int r = i / cols;
			int c = i % cols;

			// Final offset for this unit’s cell
			Vector3 offset = baseOffset + new Vector3(
				c * unitSpacing,
				0f,
				r * unitSpacing
			);

			// Send it its own target
			Vector3 worldTarget = center + offset;
			unit.SetMoveTarget(worldTarget);
		}
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
