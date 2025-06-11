using Godot;

public partial class MiniMap : Control
{
    [Export] public Vector2 MapSize;
    private Color _backgroundColor = new Color("#171717");
    private Color _friendlyUnitsColor = new Color("#38a7f1");
    private Color _cameraRectColor = Colors.White;
    private float _defaultHeight = 10f;
    private float _baseNearDist = 1.0f;
    private float _baseFarLength = 20.0f;
    private Vector2 _worldMin;
    private Vector2 _worldMax;

    public override void _Ready()
    {
        Utils.NullExportCheck(MapSize);
        _worldMin = -MapSize;
        _worldMax = MapSize;
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
        foreach (Unit u in GetTree().GetNodesInGroup(MyEnums.Group.Units.ToString()))
        {
            Vector2 worldPos = new Vector2(u.GlobalPosition.X, u.GlobalPosition.Z);
            Vector2 localPos = (worldPos - _worldMin) * scale;
            DrawCircle(localPos, 3, _friendlyUnitsColor);
        }

        // Draw camera view
        DrawCameraRect(scale);
    }

    private void DrawCameraRect(Vector2 scale)
    {
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        // Zoom factor by camera height
        float currentHeight = camera.GlobalTransform.Origin.Y;
        float zoomScale = currentHeight / _defaultHeight;

        float nearDist = _baseNearDist * zoomScale;
        float farDist = nearDist + _baseFarLength * zoomScale;

        // FOV in radians
        float fovRad = Mathf.DegToRad(camera.Fov);
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

        Vector3 cam3d = camera.GlobalTransform.Origin;
        Vector2 cam2d = new Vector2(cam3d.X, cam3d.Z);
        float yaw = camera.GlobalTransform.Basis.GetEuler().Y;

        // Transform and draw
        for (int i = 0; i < 4; i++)
        {
            Vector2 corner0 = RotateVector2(localCorners[i], -yaw) + cam2d;
            Vector2 corner1 = RotateVector2(localCorners[(i + 1) % 4], -yaw) + cam2d;
            Vector2 pixel0 = (corner0 - _worldMin) * scale;
            Vector2 pixel1 = (corner1 - _worldMin) * scale;
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
