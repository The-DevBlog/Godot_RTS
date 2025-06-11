using Godot;

public partial class MiniMap : Control
{
    [Export] public Vector2 MapSize;
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

        // Draw the boundary box
        DrawRect(new Rect2(Vector2.Zero, Size), _backgroundColor, true);

        // Draw each unit as a blue dot
        foreach (Unit u in GetTree().GetNodesInGroup(MyEnums.Group.Units.ToString()))
        {
            // world position (x,z) → minimap (u,v)
            Vector2 worldPos = new Vector2(u.GlobalPosition.X, u.GlobalPosition.Z);
            Vector2 localPos = (worldPos - _worldMin) * scale;
            DrawCircle(localPos, 3, _friendlyUnitsColor);
        }

        DrawCameraRect();
    }

    private void DrawCameraRect()
    {
        // 1. world-bounds of your minimap
        Vector2 worldRange = _worldMax - _worldMin;

        // 2. how many pixels = one world-unit?
        Vector2 controlSize = Size;
        Vector2 scale = controlSize / worldRange;

        // 3. get the active Camera3D
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null)
            return; // no camera, nothing to draw

        // 4. camera position and rotation
        Vector3 cam3d = camera.GlobalTransform.Origin;
        Vector2 cam2d = new Vector2(cam3d.X, cam3d.Z);
        Vector3 cameraRotation = camera.GlobalTransform.Basis.GetEuler();
        float yRotation = cameraRotation.Y; // Y axis rotation (yaw)
        float xRotation = cameraRotation.X; // X axis rotation (pitch)

        // 5. Create a fixed-size trapezoid to represent camera view
        // These are fixed world-space distances that won't change with zoom
        float nearWidth = 40.0f;  // Width of trapezoid at the near end (wider)
        float farWidth = 25.0f;    // Width of trapezoid at the far end (narrower)
        float length = 15.0f;     // Length of the trapezoid

        // Adjust trapezoid shape based on camera pitch
        // More pitch = more elongated trapezoid
        float pitchFactor = 1.0f + (Mathf.Abs(xRotation) / (Mathf.Pi * 0.5f)) * 0.5f; // 0.5f controls how much pitch affects length
        length *= pitchFactor;

        // 6. Create trapezoid vertices in local camera space (before rotation)
        Vector2[] localCorners = new Vector2[4];

        // Near edge (closer to camera - wider)
        localCorners[0] = new Vector2(-nearWidth * 0.5f, 0);      // near left
        localCorners[1] = new Vector2(nearWidth * 0.5f, 0);       // near right

        // Far edge (further from camera - narrower)  
        localCorners[2] = new Vector2(farWidth * 0.5f, length);   // far right
        localCorners[3] = new Vector2(-farWidth * 0.5f, length);  // far left

        // 7. Rotate trapezoid by camera's Y rotation and translate to world position
        Vector2[] worldCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            // Rotate by camera's yaw (negate for correct minimap orientation)
            Vector2 rotatedCorner = RotateVector2(localCorners[i], -yRotation);
            // Translate to camera's world position
            worldCorners[i] = cam2d + rotatedCorner;
        }

        // 8. Convert world corners to minimap pixel coordinates
        Vector2[] pixelCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            pixelCorners[i] = (worldCorners[i] - _worldMin) * scale;
        }

        // 9. Draw the trapezoid
        for (int i = 0; i < 4; i++)
        {
            int nextIndex = (i + 1) % 4;
            DrawLine(pixelCorners[i], pixelCorners[nextIndex], _cameraRectColor, 2.0f);
        }
    }

    private Vector2 RotateVector2(Vector2 vector, float angleRadians)
    {
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);

        return new Vector2(
            vector.X * cos - vector.Y * sin,
            vector.X * sin + vector.Y * cos
        );
    }
}
