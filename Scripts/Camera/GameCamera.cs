using Godot;

public partial class GameCamera : Node3D
{
	[Export] public float PanSpeed = 0.2f;
	[Export] public float PanSpeedBoost = 2.0f;
	[Export] public float RotateSpeed = 1.2f;
	[Export] public float ZoomSpeed = 2.0f;
	[Export(PropertyHint.Range, "0.01,0.4")] public float Smoothness = 0.1f;
	[Export] public float MinZoom = -5.0f;
	[Export] public float MaxZoom = 20.0f;
	[Export] public float MouseSensitivity = 0.2f;
	[Export] public float EdgeSize = 3.0f;
	[Export] public Vector2 MapSize;

	private Node3D _zoomPivot;
	private Camera3D _camera;
	private Vector3 _moveTarget;
	private float _rotateKeysTarget = 0.0f;
	private float _zoomTarget = 0.0f;

	public override void _Ready()
	{
		_zoomPivot = GetNode<Node3D>("CameraZoomPivot");
		_camera = GetNode<Camera3D>("CameraZoomPivot/Camera3D");

		_moveTarget = Position;
		_rotateKeysTarget = RotationDegrees.Y;
		_zoomTarget = _camera.Position.Z;

		Utils.NullExportCheck(MapSize);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motionEvent && Input.IsActionPressed("rotate"))
			_rotateKeysTarget -= (float)(motionEvent.Relative.X * MouseSensitivity);
	}

	public override void _Process(double delta)
	{
		HideMouseIfRotating();
		MouseEdgeScroll();
		KeyboardScroll();
		UpdateCameraPosition();
	}

	private void HideMouseIfRotating()
	{
		if (Input.IsActionJustPressed("rotate"))
			Input.MouseMode = Input.MouseModeEnum.Captured;

		if (Input.IsActionJustReleased("rotate"))
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void MouseEdgeScroll()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		Vector3 scrollDirection = Vector3.Zero;

		if (mousePos.X < EdgeSize)
			scrollDirection.X = -1;
		else if (mousePos.X > viewportSize.X - EdgeSize)
			scrollDirection.X = 1;

		if (mousePos.Y < EdgeSize)
			scrollDirection.Z = -1;
		else if (mousePos.Y > viewportSize.Y - EdgeSize)
			scrollDirection.Z = 1;

		_moveTarget += Transform.Basis * scrollDirection * PanSpeed;
		ClampMoveTarget();
	}

	private void KeyboardScroll()
	{
		Vector2 inputDirection = Input.GetVector("left", "right", "up", "down");
		Vector3 movementDirection = Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y);

		float rotateKeys = Input.GetAxis("rotate_left", "rotate_right");
		int zoomDirection = (Input.IsActionJustReleased("camera_zoom_out") ? 1 : 0)
							- (Input.IsActionJustReleased("camera_zoom_in") ? 1 : 0);

		var panSpeedBoost = Input.IsActionPressed("pan_speed_boost") ? PanSpeedBoost : 1.0f;

		_moveTarget += movementDirection * PanSpeed * panSpeedBoost;
		_rotateKeysTarget += rotateKeys * RotateSpeed;

		if (!Resources.Instance.IsPlacingStructure)
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
		Vector3 camPos = _camera.Position;
		camPos.Z = Mathf.Lerp(camPos.Z, _zoomTarget, 0.1f);
		_camera.Position = camPos;
	}

	private void ClampMoveTarget()
	{
		// Clamp X/Z within defined bounds
		_moveTarget.X = Mathf.Clamp(_moveTarget.X, -MapSize.X / 2, MapSize.X / 2);
		_moveTarget.Z = Mathf.Clamp(_moveTarget.Z, -MapSize.Y / 2, MapSize.Y / 2);
	}
}
