using Godot;

public partial class GameCamera : Node3D
{
	[Export] public float PanSpeed = 0.2f;
	[Export] public float PanSpeedBoost = 2.0f;
	[Export] public float RotateSpeed = 1.2f;
	[Export] public float ZoomSpeed = 2.0f;
	[Export(PropertyHint.Range, "0.01,0.4")] public float Smoothness = 0.1f;
	[Export] public float MinZoom = 0.0f;
	[Export] public float MaxZoom = 40.0f;
	[Export] public float MouseSensitivity = 0.2f;
	[Export] public float EdgeSize = 3.0f;
	[Export] public Camera3D Camera { get; private set; }
	private Vector2 _mapSize;
	private Node3D _zoomPivot;
	private Vector3 _moveTarget;
	private float _rotateKeysTarget = 0.0f;
	private float _zoomTarget = 0.0f;
	private GlobalResources _globalResources;

	public override void _Ready()
	{
		Utils.NullExportCheck(Camera);

		_globalResources = GlobalResources.Instance;
		_zoomPivot = GetNode<Node3D>("CameraZoomPivot");
		_mapSize = GlobalResources.Instance.MapSize;
		_moveTarget = Position;
		_rotateKeysTarget = RotationDegrees.Y;
		_zoomTarget = Camera.Position.Z;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motionEvent && Input.IsActionPressed("rotate"))
		{
			_rotateKeysTarget -= (float)(motionEvent.Relative.X * MouseSensitivity);
		}

		HideMouseIfRotating(@event);
	}

	public override void _Process(double delta)
	{
		MouseEdgeScroll(delta);
		KeyboardScroll(delta);
		UpdateCameraPosition();
	}

	public void SetCameraTarget(Vector2 worldXZ)
	{
		_moveTarget.X = worldXZ.X;
		_moveTarget.Z = worldXZ.Y;
		ClampMoveTarget(); // Optional, depending on your bounds
	}

	private void HideMouseIfRotating(InputEvent @event)
	{
		if (Input.IsActionJustPressed("rotate"))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}

		if (Input.IsActionJustReleased("rotate"))
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void MouseEdgeScroll(double delta)
	{
		var viewport = GetViewport();
		Vector2 mousePos = viewport.GetMousePosition();
		Vector2 viewportSize = viewport.GetVisibleRect().Size;
		Vector3 scrollDirection = Vector3.Zero;

		// → If the mouse is truly outside the window, bail out
		if (mousePos.X < 0 || mousePos.X > viewportSize.X || mousePos.Y < 0 || mousePos.Y > viewportSize.Y)
			return;

		if (mousePos.X < EdgeSize)
			scrollDirection.X = -1;
		else if (mousePos.X > viewportSize.X - EdgeSize)
			scrollDirection.X = 1;

		if (mousePos.Y < EdgeSize)
			scrollDirection.Z = -1;
		else if (mousePos.Y > viewportSize.Y - EdgeSize)
			scrollDirection.Z = 1;

		_moveTarget += Transform.Basis * scrollDirection * PanSpeed * (float)delta;
		ClampMoveTarget();
	}

	private void KeyboardScroll(double delta)
	{
		Vector2 inputDirection = Input.GetVector("left", "right", "up", "down");
		Vector3 movementDirection = Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y);

		float rotateKeys = Input.GetAxis("rotate_left", "rotate_right");
		int zoomDirection = (Input.IsActionJustReleased("camera_zoom_out") ? 1 : 0)
							- (Input.IsActionJustReleased("camera_zoom_in") ? 1 : 0);

		var panSpeedBoost = Input.IsActionPressed("pan_speed_boost") ? PanSpeedBoost : 1.0f;

		_moveTarget += movementDirection * PanSpeed * panSpeedBoost * (float)delta;
		_rotateKeysTarget += rotateKeys * RotateSpeed * (float)delta;

		if (!GlobalResources.Instance.IsPlacingStructure)
			_zoomTarget = Mathf.Clamp(_zoomTarget + zoomDirection * ZoomSpeed, MinZoom, MaxZoom);

		ClampMoveTarget();
	}

	private void UpdateCameraPosition()
	{
		// Smooth movement
		Position = Position.Lerp(_moveTarget, Smoothness);

		// Smooth rotation
		Vector3 rotation = RotationDegrees;
		rotation.Y = Mathf.Lerp(rotation.Y, _rotateKeysTarget, Smoothness);
		RotationDegrees = rotation;

		// Smooth zoom
		Vector3 camPos = Camera.Position;
		camPos.Z = Mathf.Lerp(camPos.Z, _zoomTarget, 0.1f);
		Camera.Position = camPos;
	}

	private void ClampMoveTarget()
	{
		// Clamp X/Z within defined bounds
		_moveTarget.X = Mathf.Clamp(_moveTarget.X, -_mapSize.X / 2, _mapSize.X / 2);
		_moveTarget.Z = Mathf.Clamp(_moveTarget.Z, -_mapSize.Y / 2, _mapSize.Y / 2);
	}
}
