using Godot;

public partial class MiniMap : Control
{
    [Export] public Vector2 MapSize;
    private Color _backgroundColor = new Color("#171717");
    private Color _friendlyUnitsColor;
    private Color _cameraRectColor = Colors.White;
    private float _defaultHeight = 10f;
    private float _baseNearDist = 1.0f;
    private float _baseFarLength = 20.0f;
    private Vector2 _worldMin;
    private Vector2 _worldMax;
    private Camera3D _camera;
    private Resources _resources;

    public override void _Ready()
    {
        Utils.NullExportCheck(MapSize);
        _worldMin = -MapSize / 2;
        _worldMax = MapSize / 2;
        _camera = GetViewport().GetCamera3D();
        _resources = Resources.Instance;
        _friendlyUnitsColor = _resources.TeamColor;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 mapSize = _worldMax - _worldMin;
        Vector2 controlSize = Size;
        Vector2 scale = controlSize / mapSize;

        // Draw background
        DrawRect(new Rect2(Vector2.Zero, Size), _backgroundColor, true);

        // Draw units
        var units = GetTree().GetNodesInGroup(MyEnums.Group.Units.ToString().ToLower());
        foreach (Unit unit in units)
        {
            Vector2 worldPos = new Vector2(unit.GlobalPosition.X, unit.GlobalPosition.Z);
            Vector2 localPos = (worldPos - _worldMin) * scale;
            DrawCircle(localPos, 3, _friendlyUnitsColor);
        }

        // Draw structures
        var structures = GetTree().GetNodesInGroup(MyEnums.Group.Structures.ToString().ToLower());
        foreach (StructureBase structure in structures)
        {
            Vector2 worldPos = new Vector2(structure.GlobalPosition.X, structure.GlobalPosition.Z);
            Vector2 localPos = (worldPos - _worldMin) * scale;
            DrawCircle(localPos, 3, _friendlyUnitsColor);
        }

        // Draw camera view
        DrawCameraRect(scale);
    }

    private void DrawCameraRect(Vector2 scale)
    {
        if (_camera == null) return;

        // Zoom factor by camera height
        float currentHeight = _camera.GlobalTransform.Origin.Y;
        float zoomScale = currentHeight / _defaultHeight;

        float nearDist = _baseNearDist * zoomScale;
        float farDist = nearDist + _baseFarLength * zoomScale;

        // FOV in radians
        float fovRad = Mathf.DegToRad(_camera.Fov);
        float nearWidth = 2f * nearDist * Mathf.Tan(fovRad * 0.5f) * 10.0f;
        float farWidth = 2f * farDist * Mathf.Tan(fovRad * 0.5f);

        // Build trapezoid in camera-local space (negative Y goes 'up' on minimap)
        Vector2[] localCorners =
        [
            new Vector2(-nearWidth / 2f, -nearDist), // near left
			new Vector2(nearWidth / 2f, -nearDist), // near right
			new Vector2(farWidth / 2f, -farDist),  // far right
			new Vector2(-farWidth / 2f, -farDist),  // far left
		];

        Vector3 cam3d = _camera.GlobalTransform.Origin;
        Vector2 cam2d = new Vector2(cam3d.X, cam3d.Z);
        float yaw = _camera.GlobalTransform.Basis.GetEuler().Y;

        // Transform, clamp within world bounds, and draw
        for (int i = 0; i < 4; i++)
        {
            // Rotate into world coords
            Vector2 worldCorner0 = RotateVector2(localCorners[i], -yaw) + cam2d;
            Vector2 worldCorner1 = RotateVector2(localCorners[(i + 1) % 4], -yaw) + cam2d;

            // Clamp to minimap world limits
            worldCorner0.X = Mathf.Clamp(worldCorner0.X, _worldMin.X, _worldMax.X);
            worldCorner0.Y = Mathf.Clamp(worldCorner0.Y, _worldMin.Y, _worldMax.Y);
            worldCorner1.X = Mathf.Clamp(worldCorner1.X, _worldMin.X, _worldMax.X);
            worldCorner1.Y = Mathf.Clamp(worldCorner1.Y, _worldMin.Y, _worldMax.Y);

            // Convert to pixel coords
            Vector2 pixel0 = (worldCorner0 - _worldMin) * scale;
            Vector2 pixel1 = (worldCorner1 - _worldMin) * scale;
            DrawLine(pixel0, pixel1, _cameraRectColor, 2f);
        }
    }

    private Vector2 RotateVector2(Vector2 v, float angle)
    {
        float c = Mathf.Cos(angle);
        float s = Mathf.Sin(angle);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
}
