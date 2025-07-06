using System.Collections.Generic;
using System.Linq;
using Godot;
using MyEnums;

public partial class UnitCommands : Player
{
	private const float MIN_DRAG_DIST = 10f;
	[Export] private Player _player;
	private HashSet<Unit> _selectedUnits;
	private Resources _resources;
	private Camera3D _camera;
	private Vector2 _dragStart = Vector2.Zero;
	private Vector2 _dragEnd = Vector2.Zero;
	private bool _mouseDown;
	private bool _dragActive;
	private bool _isAnySelected;

	public override void _Ready()
	{
		GD.Print("UnitCommands _Ready() called");

		_resources = Resources.Instance;
		Utils.NullCheck(_resources);

		_camera = _resources.Camera;
		Utils.NullCheck(_camera);

		// _camera = GetViewport().GetCamera3D();
		_selectedUnits = new HashSet<Unit>();
		// _player = PlayerManager.Instance.LocalPlayer;

		// Utils.NullCheck(_player);
		// Utils.NullCheck(_camera);
		// Utils.NullCheck(_resources);
		// Utils.NullCheck(_selectedUnits);

		// // _player.DeselectAllUnits += OnDeselectAllUnits;
		// DeselectAllUnits += OnDeselectAllUnits;

		// if (_camera == null)
		// 	Utils.PrintErr("Camera3D not found.");
		GD.Print("UnitCommands _Ready() completed");
	}

	public override void _Process(double delta)
	{
		_isAnySelected = _selectedUnits.Count > 0;
		HandleMouseInput();
	}

	public override void _Draw()
	{
		DrawDragSelectRect();
	}

	private void DrawDragSelectRect()
	{
		if (!_dragActive)
			return;

		var rect = new Rect2(_dragStart, _dragEnd - _dragStart).Abs();
		DrawRect(rect, new Color(0.2f, 0.6f, 1.0f, 0.3f), filled: true);
		DrawRect(rect, new Color(0.2f, 0.6f, 1.0f), filled: false, width: 2);
	}

	private void HandleMouseInput()
	{
		// if (_player.IsPlacingStructure)
		if (IsPlacingStructure)
			return;

		if (Input.IsActionJustReleased("mb_secondary"))
			_player.EmitSignal(nameof(DeselectAllUnits));
		// _player.EmitSignal(nameof(_player.DeselectAllUnits));

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
			if (_dragActive)
				EmitSelectUnits(_selectedUnits.ToArray());
			// _player.EmitSelectUnits(_selectedUnits.ToArray());

			Vector2 mousePosition = GetViewport().GetMousePosition();

			bool isHit = SelectSingleUnit(mousePosition);

			if (!_dragActive && _isAnySelected && !isHit)
				SetTargetPosition(mousePosition);

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

	private bool SelectSingleUnit(Vector2 mousePosition)
	{
		if (IsHoveringUI)
			// if (_player.IsHoveringUI)
			return false;

		Vector3 from = _camera.ProjectRayOrigin(mousePosition);
		Vector3 to = from + _camera.ProjectRayNormal(mousePosition) * 1000f;

		var query = new PhysicsRayQueryParameters3D { From = from, To = to };
		var spaceState = _camera.GetWorld3D().DirectSpaceState;
		var result = spaceState.IntersectRay(query);

		if (result.Count == 0)
			return false;

		CollisionObject3D collider = (CollisionObject3D)result["collider"];

		if (collider != null && collider.IsInGroup(Group.Units.ToString()))
		{
			foreach (Unit u in _selectedUnits)
				u.Selected = false;

			Unit unit = FindAncestor<Unit>(collider);
			unit.Selected = true;
			_selectedUnits = new HashSet<Unit>() { unit };

			// _player.EmitSelectUnits(_selectedUnits.ToArray());
			EmitSelectUnits(_selectedUnits.ToArray());

			return true;
		}

		return false;
	}

	private static T FindAncestor<T>(Node node) where T : Node
	{
		while (node != null)
		{
			if (node is T t)
				return t;
			node = node.GetParent();
		}
		return null;
	}

	private void SetTargetPosition(Vector2 mousePos)
	{
		// if (_player.IsHoveringUI)
		if (IsHoveringUI)
			return;

		Vector3 from = _camera.ProjectRayOrigin(mousePos);
		Vector3 to = from + _camera.ProjectRayNormal(mousePos) * 1000f;

		var rayParams = new PhysicsRayQueryParameters3D
		{
			From = from,
			To = to
		};

		var result = _camera.GetWorld3D().DirectSpaceState.IntersectRay(rayParams);
		if (!result.TryGetValue("position", out var hitVar))
		{
			GD.Print("Invalid target location");
			return;
		}

		Vector3 center = hitVar.AsVector3();

		// collect selected units
		var selectedUnits = GetTree()
			.GetNodesInGroup(Group.Units.ToString())
			.OfType<Unit>()
			.Where(u => u.Selected)
			.ToArray();

		int count = selectedUnits.Length;
		if (count == 0)
		{
			GD.Print("No selected units");
			return;
		}

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

	private void OnDeselectAllUnits()
	{
		if (!_isAnySelected)
			return;

		GD.Print("Deselect all units");

		foreach (Unit unit in _selectedUnits)
			unit.Selected = false;

		_selectedUnits = new HashSet<Unit>();
		_dragActive = false;

		EmitSelectUnits(_selectedUnits.ToArray());
		// _player.EmitSelectUnits(_selectedUnits.ToArray());
	}

	// Marks units as selected or unselected.
	// The _prevSelectedUnits set is used to track the previous state of selection. (more efficient than looping through all units)
	private void MarkSelectedUnits(List<Unit> units)
	{
		var selectedUnits = new HashSet<Unit>(units);

		foreach (Unit unit in _selectedUnits)
		{
			if (!selectedUnits.Contains(unit))
				unit.Selected = false;
		}

		foreach (Unit unit in selectedUnits)
		{
			if (!_selectedUnits.Contains(unit))
				unit.Selected = true;
		}

		_selectedUnits = selectedUnits;
	}
}
