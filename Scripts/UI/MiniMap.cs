using System.Collections.Generic;
using Godot;

public partial class MiniMap : Control
{
    private GameCamera _gameCamera { get; set; }
    private Vector2 _mapSize;
    private Color _backgroundColor = new Color("#171717");
    private Color _friendlyUnitsColor;
    private Color _cameraRectColor = Colors.White;
    private float _defaultHeight = 5f;
    private float _baseNearDist = 1.0f;
    private float _baseFarLength = 20.0f;
    private Vector2 _worldMin;
    private Vector2 _worldMax;
    private Camera3D _camera;
    private GlobalResources _globalResources;
    private Player _player;
    private readonly List<Unit> _units = new();
    private readonly List<StructureBase> _structures = new();
    private static readonly float SQRT2 = 1.41421356237f;
    private readonly Vector2[] _unitCorners = new Vector2[4];
    private readonly Color[] _unitColors = { Colors.White, Colors.White, Colors.White, Colors.White };
    private const double MiniMapFPS = 45.0;
    private double _targetDelta = 1.0 / MiniMapFPS;
    private double _accum;

    public override void _EnterTree()
    {
        GetTree().NodeAdded += OnNodeAdded;
        GetTree().NodeRemoved += OnNodeRemoved;
    }

    public override void _Ready()
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager.HumanPlayer != null)
            _player = playerManager.HumanPlayer;
        else
            playerManager.HumanPlayerReady += OnHumanPlayerReady;

        Utils.NullCheck(_player);

        _globalResources = GlobalResources.Instance;
        _gameCamera = _globalResources.GameCamera;
        _mapSize = _globalResources.MapSize;
        _worldMin = -_mapSize / 2;
        _worldMax = _mapSize / 2;
        _camera = GetViewport().GetCamera3D();
        _friendlyUnitsColor = _player.Color;

        Utils.NullCheck(_gameCamera);
    }

    public override void _Process(double delta)
    {
        _accum += delta;

        // avoid huge catch-up after a long hitch
        if (_accum >= _targetDelta * 2.0)
            _accum = _targetDelta;

        if (_accum >= _targetDelta)
        {
            _accum = 0;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        Vector2 mapSize = _worldMax - _worldMin;
        Vector2 controlSize = Size;
        Vector2 scale = controlSize / mapSize;

        // Draw background
        DrawRect(new Rect2(Vector2.Zero, Size), _backgroundColor, true);
        DrawUnits(scale);
        DrawStructures(scale);
        DrawCameraRect(scale);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb &&
            mb.ButtonIndex == MouseButton.Left &&
            mb.Pressed)
        {
            NavigateToPostition();
        }
    }

    // Add units and structures to their respective lists when they are added to the scene
    private void OnNodeAdded(Node n)
    {
        if (n is Unit u && u.IsInGroup("units")) _units.Add(u);
        if (n is StructureBase s && s.IsInGroup("structures")) _structures.Add(s);
    }

    // Remove units and structures from their respective lists when they are removed from the scene
    private void OnNodeRemoved(Node n)
    {
        if (n is Unit u) _units.Remove(u);
        if (n is StructureBase s) _structures.Remove(s);
    }

    private void OnHumanPlayerReady(Player player)
    {
        _player = player;
    }

    // Jump camera position to the clicked point on the minimap
    private void NavigateToPostition()
    {
        // 1) get the click position inside the minimap
        Vector2 localPos = GetLocalMousePosition();

        // 2) compute percentage across the control
        Vector2 percent = new Vector2(
            localPos.X / Size.X,
            localPos.Y / Size.Y
        );

        // 3) map that to world coords
        Vector2 mapSize = _worldMax - _worldMin;
        Vector2 worldXZ = _worldMin + percent * mapSize;

        // 4) tell your camera to move there
        _gameCamera.SetCameraTarget(worldXZ);
    }

    private void DrawUnits(Vector2 scale)
    {
        float sx = scale.X; // use X scale for size; good enough for a minimap
        _unitColors[0] = _unitColors[1] = _unitColors[2] = _unitColors[3] = _friendlyUnitsColor;

        foreach (Unit unit in _units)
        {
            // world → minimap pixel center
            Vector2 wp = new(unit.GlobalPosition.X, unit.GlobalPosition.Z);
            if (wp.X < _worldMin.X || wp.X > _worldMax.X || wp.Y < _worldMin.Y || wp.Y > _worldMax.Y)
                continue;

            Vector2 rel = wp - _worldMin;
            Vector2 center = new(Mathf.Round(rel.X * sx), Mathf.Round(rel.Y * scale.Y));

            // choose the square’s half-side in pixels (map your cached world radius however you like)
            float halfSidePx = Mathf.Max(1.0f, unit.MiniMapRadius * sx);
            float halfDiagPx = halfSidePx * SQRT2; // distance from center to each corner

            // 45° square (diamond): C + (0,±d), C + (±d,0)
            _unitCorners[0] = new(center.X, center.Y - halfDiagPx);
            _unitCorners[1] = new(center.X + halfDiagPx, center.Y);
            _unitCorners[2] = new(center.X, center.Y + halfDiagPx);
            _unitCorners[3] = new(center.X - halfDiagPx, center.Y);

            DrawPolygon(_unitCorners, _unitColors);

            // optional (slower): outline
            // for (int i = 0; i < 4; i++)
            //     DrawLine(_squareCorners[i], _squareCorners[(i + 1) % 4], Colors.Black, 1f);
        }
    }


    private void DrawStructures(Vector2 scale)
    {
        // Draw structures
        foreach (StructureBase structure in _structures)
        {
            Vector2 worldPos = new Vector2(structure.GlobalPosition.X, structure.GlobalPosition.Z);
            Vector2 localPos = (worldPos - _worldMin) * scale;

            // Default rectangle size
            Vector2 rectSize = new Vector2(6, 6);

            var collider = structure.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
            if (collider != null && collider.Shape is BoxShape3D box)
            {
                rectSize = new Vector2(box.Size.X * scale.X, box.Size.Z * scale.Y);
            }
            else if (collider != null && collider.Shape is SphereShape3D sphere)
            {
                float diameter = sphere.Radius * 2f;
                rectSize = new Vector2(diameter * scale.X, diameter * scale.Y);
            }

            // Get the structure's yaw (Y rotation)
            float yaw = structure.GlobalTransform.Basis.GetEuler().Y;

            // Rectangle corners centered at (0,0)
            Vector2 half = rectSize / 2f;
            Vector2[] structureCorners =
            [
                new Vector2(-half.X, -half.Y),
                new Vector2( half.X, -half.Y),
                new Vector2( half.X,  half.Y),
                new Vector2(-half.X,  half.Y),
            ];

            // Rotate and translate corners
            for (int i = 0; i < 4; i++)
                structureCorners[i] = RotateVector2(structureCorners[i], -yaw) + localPos;

            // Draw the rectangle as a polygon
            DrawPolygon(structureCorners, new Color[] { _friendlyUnitsColor, _friendlyUnitsColor, _friendlyUnitsColor, _friendlyUnitsColor });

            // Draw black outline
            // for (int i = 0; i < 4; i++)
            //     DrawLine(corners[i], corners[(i + 1) % 4], Colors.Black, 1f);
        }
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
        float nearWidth = 2f * nearDist * Mathf.Tan(fovRad * 0.5f) * 15.0f;
        float farWidth = 2f * farDist * Mathf.Tan(fovRad * 0.5f) * 2.0f;

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
