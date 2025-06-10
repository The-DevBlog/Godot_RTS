using Godot;

public partial class MiniMap : Control
{
	private Color _backgbroundColor = new Color("#171717");
	private Color _friendlyUnitsColor = new Color("#38a7f1");
	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 worldMin = new Vector2(-400, -400);
		Vector2 worldMax = new Vector2(400, 400);

		Vector2 mapSize = worldMax - worldMin;
		Vector2 controlSize = Size;
		Vector2 scale = controlSize / mapSize;

		// Draw the boundary box
		DrawRect(new Rect2(Vector2.Zero, Size), _backgbroundColor, true, 2);

		// Draw each unit as a blue dot
		foreach (Unit u in GetTree().GetNodesInGroup(MyEnums.Group.Units.ToString()))
		{
			// world position (x,z) → minimap (u,v)
			Vector2 worldPos = new Vector2(u.GlobalPosition.X, u.GlobalPosition.Z);
			Vector2 localPos = (worldPos - worldMin) * scale;
			DrawCircle(localPos, 3, _friendlyUnitsColor);
		}

		// (Optionally) draw enemies in red, resources in green, etc.
	}
}
