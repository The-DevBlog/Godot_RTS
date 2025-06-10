using Godot;

public partial class MiniMap : Control
{
    [Export] public Vector2 MapSize;
    private Color _backgbroundColor = new Color("#171717");
    private Color _friendlyUnitsColor = new Color("#38a7f1");
    private Color _cameraRectColor = new Color("#ff0000");
    public override void _Process(double delta)
    {
        Utils.NullExportCheck(MapSize);

        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 worldMin = -MapSize;
        Vector2 worldMax = MapSize;

        Vector2 mapSize = worldMax - worldMin;
        Vector2 controlSize = Size;
        Vector2 scale = controlSize / mapSize;

        // Draw the boundary box
        DrawRect(new Rect2(Vector2.Zero, Size), _backgbroundColor, true);

        // Draw each unit as a blue dot
        foreach (Unit u in GetTree().GetNodesInGroup(MyEnums.Group.Units.ToString()))
        {
            // world position (x,z) → minimap (u,v)
            Vector2 worldPos = new Vector2(u.GlobalPosition.X, u.GlobalPosition.Z);
            Vector2 localPos = (worldPos - worldMin) * scale;
            DrawCircle(localPos, 3, _friendlyUnitsColor);
        }

        DrawCameraRect();
    }

    private void DrawCameraRect()
    {
        // 1. world-bounds of your minimap
        Vector2 worldMin = -MapSize;
        Vector2 worldMax = MapSize;
        Vector2 worldRange = worldMax - worldMin;

        // 2. how many pixels = one world-unit?
        Vector2 controlSize = Size;                   // your Control’s size in pixels
        Vector2 scale = controlSize / worldRange;

        // 3. get the active Camera3D
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null)
            return; // no camera, nothing to draw

        // 4. camera position on the XZ plane
        Vector3 cam3d = camera.GlobalTransform.Origin;
        Vector2 cam2d = new Vector2(cam3d.X, cam3d.Z);

        // 5. compute half-extents of the view-rectangle (in world units)
        Vector2 camHalf;
        // altitude * tan(FOV/2) on the ground plane
        float altitude = cam3d.Y;
        float fovRad = Mathf.DegToRad(camera.Fov);
        float halfH = altitude * Mathf.Tan(fovRad * 0.5f);
        Vector2 vp = GetViewportRect().Size;
        float aspect = vp.X / vp.Y;
        float halfW = halfH * aspect;
        camHalf = new Vector2(halfW, halfH);

        // 6. world-space corners of camera view
        Vector2 camWorldMin = cam2d - camHalf;
        Vector2 camWorldMax = cam2d + camHalf;

        // 7. convert into minimap-local (pixel) coords
        Vector2 camLocalMin = (camWorldMin - worldMin) * scale;
        Vector2 camLocalSize = (camWorldMax - camWorldMin) * scale;

        // 8. draw just the outline
        DrawRect(new Rect2(camLocalMin, camLocalSize), _cameraRectColor, false);
    }

}
