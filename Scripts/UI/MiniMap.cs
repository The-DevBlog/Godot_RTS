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
        // 1) world‐bounds of your minimap
        Vector2 worldMin = -MapSize;
        Vector2 worldMax = MapSize;
        Vector2 worldRange = worldMax - worldMin;

        // 2) scale: world‐units → control‐pixels
        Vector2 controlSize = Size;
        Vector2 scale = controlSize / worldRange;

        // 3) get the active Camera3D
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null)
            return;

        // 4) camera position on the XZ plane
        Vector3 cam3d = camera.GlobalTransform.Origin;
        Vector2 cam2d = new Vector2(cam3d.X, cam3d.Z);

        // 5) compute half‐extents of the view‐rectangle (in world units)
        Vector2 camHalf;
        if (camera.Projection == Camera3D.ProjectionType.Orthogonal)
        {
            // orthographic: use the camera’s Size (half‐height) + aspect for width
            float halfH = camera.Size;
            Vector2 vp = GetViewportRect().Size;
            float aspect = vp.x / vp.;
            float halfW = halfH * aspect;
            camHalf = new Vector2(halfW, halfH);
        }
        else
        {
            // perspective: at “straight‐down” pitch the footprint is
            // altitude * tan(FOV/2) on the ground plane
            float altitude = cam3d.y;
            float fovRad = Mathf.DegToRad(camera.Fov);
            float halfH = altitude * Mathf.Tan(fovRad * 0.5f);
            Vector2 vp = GetViewportRect().Size;
            float aspect = vp.x / vp.y;
            float halfW = halfH * aspect;
            camHalf = new Vector2(halfW, halfH);
        }

        // 6) extract camera’s yaw (rotation around Y)
        //    in Godot 4, Rotation is a Vector3 of Euler angles (YXZ order), so:
        float yaw = camera.Rotation.y;

        // 7) build the two local axes in world‐XZ
        Vector2 right = new Vector2(Mathf.Cos(yaw), Mathf.Sin(yaw));
        Vector2 up = new Vector2(-Mathf.Sin(yaw), Mathf.Cos(yaw));

        // 8) compute the 4 world‐space corners of the oriented rect
        Vector2 center = cam2d;
        Vector2 halfWv = right * camHalf.x;
        Vector2 halfHv = up * camHalf.y;

        Vector2 w1 = center + halfWv + halfHv;
        Vector2 w2 = center - halfWv + halfHv;
        Vector2 w3 = center - halfWv - halfHv;
        Vector2 w4 = center + halfWv - halfHv;

        // 9) convert each corner into minimap (pixel) coords
        Vector2[] local = new Vector2[4];
        Vector2[] world = new[] { w1, w2, w3, w4 };
        for (int i = 0; i < 4; i++)
            local[i] = (world[i] - worldMin) * scale;

        // 10) draw the outline
        for (int i = 0; i < 4; i++)
            DrawLine(local[i], local[(i + 1) % 4], _cameraRectColor, 2);
    }
}
