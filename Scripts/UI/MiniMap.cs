using Godot;

public partial class MiniMap : Control
{
    [Export] public Vector2 MapSize;
    [Export(PropertyHint.Range, "1,120,0.1")] public float DefaultFov = 70f;
    [Export(PropertyHint.Range, "1,100,0.1")] public float DefaultHeight = 10f; // Camera height at neutral zoom
    private const float BaseNearDist = 1.0f;
    private const float BaseFarLength = 20.0f;

    private Color _backgroundColor = new Color("#171717");
    private Color _friendlyUnitsColor = new Color("#38a7f1");
    private Color _cameraRectColor = new Color("#ff0000");
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

        // Background
        DrawRect(new Rect2(Vector2.Zero, Size), _backgroundColor, true);

        // Units
        foreach (Unit u in GetTree().GetNodesInGroup(MyEnums.Group.Units.ToString()))
        {
            Vector2 worldPos = new Vector2(u.GlobalPosition.X, u.GlobalPosition.Z);
            Vector2 localPos = (worldPos - _worldMin) * scale;
            DrawCircle(localPos, 3, _friendlyUnitsColor);
        }

        DrawCameraRect(scale);
    }

    private void DrawCameraRect(Vector2 scale)
    {
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        // Determine zoom scale from camera height
        float currentHeight = camera.GlobalTransform.Origin.Y;
        float zoomScale = currentHeight / DefaultHeight;

        // Scale near/far distances by height-based zoom
        float nearDist = BaseNearDist * zoomScale;
        float farDist = nearDist + BaseFarLength * zoomScale;

        // Get FOV in radians for width calculation
        float fovRad = Mathf.DegToRad(camera.Fov);
        float nearWidth = 2f * nearDist * Mathf.Tan(fovRad * 0.5f);
        float farWidth = 2f * farDist * Mathf.Tan(fovRad * 0.5f);

        // Local corners of trapezoid
        Vector2[] localCorners = new Vector2[4];
        localCorners[0] = new Vector2(-nearWidth / 2f, nearDist);
        localCorners[1] = new Vector2(nearWidth / 2f, nearDist);
        localCorners[2] = new Vector2(farWidth / 2f, farDist);
        localCorners[3] = new Vector2(-farWidth / 2f, farDist);

        // Camera yaw & 2D position
        Vector3 cam3d = camera.GlobalTransform.Origin;
        Vector2 cam2d = new Vector2(cam3d.X, cam3d.Z);
        float yaw = camera.GlobalTransform.Basis.GetEuler().Y;

        // Transform to world and draw
        for (int i = 0; i < 4; i++)
        {
            Vector2 c0 = RotateVector2(localCorners[i], -yaw) + cam2d;
            Vector2 c1 = RotateVector2(localCorners[(i + 1) % 4], -yaw) + cam2d;
            Vector2 p0 = (c0 - _worldMin) * scale;
            Vector2 p1 = (c1 - _worldMin) * scale;
            DrawLine(p0, p1, _cameraRectColor, 2f);
        }
    }

    private Vector2 RotateVector2(Vector2 v, float angle)
    {
        float c = Mathf.Cos(angle);
        float s = Mathf.Sin(angle);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
}
