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
        Utils.NullExportCheck(MapSize);
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

        // 4. camera position on the XZ plane
        Vector3 cam3d = camera.GlobalTransform.Origin;
        Vector2 cam2d = new Vector2(cam3d.X, cam3d.Z);

        // 5. compute half-extents of the view-rectangle (in world units)
        float altitude = cam3d.Y;
        float fovRad = Mathf.DegToRad(camera.Fov);
        float halfH = altitude * Mathf.Tan(fovRad * 0.5f);
        Vector2 vp = GetViewportRect().Size;
        float aspect = vp.X / vp.Y;
        float halfW = halfH * aspect;

        // 6. Get camera's Y rotation (around vertical axis)
        Vector3 cameraRotation = camera.GlobalTransform.Basis.GetEuler();
        float yRotation = cameraRotation.Y; // Y axis rotation in radians

        // 7. Calculate the four corners of the camera view rectangle in world space
        Vector2[] corners = new Vector2[4];

        // Local corners relative to camera (before rotation)
        Vector2[] localCorners = {
            new Vector2(-halfW, -halfH), // bottom-left
            new Vector2(halfW, -halfH),  // bottom-right  
            new Vector2(halfW, halfH),   // top-right
            new Vector2(-halfW, halfH)   // top-left
        };

        // Rotate each corner and translate to world position
        for (int i = 0; i < 4; i++)
        {
            // Rotate the local corner by camera's Y rotation
            Vector2 rotatedCorner = RotateVector2(localCorners[i], yRotation);
            // Translate to world position
            corners[i] = cam2d + rotatedCorner;
        }

        // 8. Convert world corners to minimap pixel coordinates
        Vector2[] pixelCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            pixelCorners[i] = (corners[i] - _worldMin) * scale;
        }

        // 9. Draw the rotated rectangle as connected lines
        for (int i = 0; i < 4; i++)
        {
            int nextIndex = (i + 1) % 4;
            DrawLine(pixelCorners[i], pixelCorners[nextIndex], _cameraRectColor, 2.0f);
        }

        // Optional: Draw a small arrow to show camera direction
        DrawCameraDirection(cam2d, yRotation, _worldMin, scale);
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

    private void DrawCameraDirection(Vector2 cameraWorldPos, float yRotation, Vector2 worldMin, Vector2 scale)
    {
        // Convert camera position to pixel coordinates
        Vector2 camPixelPos = (cameraWorldPos - worldMin) * scale;

        // Create a forward direction vector (pointing "forward" from camera)
        Vector2 forwardDir = RotateVector2(Vector2.Up, yRotation); // Up is forward in 2D top-down view

        // Scale the direction for visibility
        Vector2 arrowEnd = camPixelPos + forwardDir * 15; // 15 pixels long

        // Draw the direction arrow
        DrawLine(camPixelPos, arrowEnd, _cameraRectColor, 3.0f);

        // Draw arrowhead
        Vector2 arrowLeft = arrowEnd + RotateVector2(Vector2.Up, yRotation + Mathf.Pi * 0.75f) * 5;
        Vector2 arrowRight = arrowEnd + RotateVector2(Vector2.Up, yRotation - Mathf.Pi * 0.75f) * 5;

        DrawLine(arrowEnd, arrowLeft, _cameraRectColor, 2.0f);
        DrawLine(arrowEnd, arrowRight, _cameraRectColor, 2.0f);
    }
}