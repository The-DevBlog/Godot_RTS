using Godot;

public partial class Camera : Node3D
{
	[Export]
	public float PanSpeed = 0.2f;
	[Export]
	public float RotateSpeed = 1.2f;
	[Export]
	public float ZoomSpeed = 2.0f;
	[Export(PropertyHint.Range, "0.01,0.4")]
	public float Smoothness = 0.1f;
	[Export]
	public float MinZoom = -5.0f;
	[Export]
	public float MaxZoom = 20.0f;
	[Export]
	public float MouseSensitivity = 0.2f;
	[Export]
	public float EdgeSize = 3.0f;

	private Node3D _rotationX;
	private Node3D _zoomPivot;
	private Camera3D _camera;
	private Vector3 _moveTarget;
	private float _rotateKeysTarget = 0.0f;
	private float _zoomTarget = 0.0f;

	public override void _Ready()
	{
		_rotationX = GetNode<Node3D>("CameraPosition/CameraRotationX");
		_zoomPivot = GetNode<Node3D>("CameraPosition/CameraRotationX/CameraZoomPivot");
		_camera = GetNode<Camera3D>("CameraPosition/CameraRotationX/CameraZoomPivot/Camera3D");

		_moveTarget = this.Position;
		_rotateKeysTarget = this.RotationDegrees.Y;
		_zoomTarget = _zoomPivot.Position.Z;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motionEvent && Input.IsActionPressed("rotate"))
		{
			_rotateKeysTarget -= (float)(motionEvent.Relative.X * MouseSensitivity);

			Vector3 rot = _rotationX.RotationDegrees;
			rot.X -= (float)(motionEvent.Relative.Y * MouseSensitivity);
			rot.X = Mathf.Clamp(rot.X, -10f, 30f);
			_rotationX.RotationDegrees = rot;
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("rotate"))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}

		if (Input.IsActionJustReleased("rotate"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		// Edge scroll
		var mousePos = GetViewport().GetMousePosition();
		var viewportSize = GetViewport().GetVisibleRect().Size;
		var scrollDirection = Vector3.Zero;

		if (mousePos.X < EdgeSize)
			scrollDirection.X = -1;
		else if (mousePos.X > viewportSize.X - EdgeSize)
			scrollDirection.X = 1;

		if (mousePos.Y < EdgeSize)
			scrollDirection.Z = -1;
		else if (mousePos.Y > viewportSize.Y - EdgeSize)
			scrollDirection.Z = 1;

		_moveTarget += this.Transform.Basis * scrollDirection * PanSpeed;

		// Get input direction
		var inputDirection = Input.GetVector("left", "right", "up", "down");
		var movementDirection = _rotationX.GlobalTransform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y);
		var rotateKeys = Input.GetAxis("rotate_left", "rotate_right");
		int zoomDirection = (Input.IsActionJustReleased("camera_zoom_out") ? 1 : 0) - (Input.IsActionJustReleased("camera_zoom_in") ? 1 : 0);

		// Set movement target
		_moveTarget += movementDirection * PanSpeed;
		_rotateKeysTarget += rotateKeys * RotateSpeed;
		_zoomTarget = Mathf.Clamp(_zoomTarget + zoomDirection * ZoomSpeed, MinZoom, MaxZoom);

		// Lerp to new position
		this.Position = Position.Lerp(_moveTarget, Smoothness);

		Vector3 rotation = RotationDegrees;
		rotation.Y = Mathf.Lerp(rotation.Y, _rotateKeysTarget, Smoothness);
		this.RotationDegrees = rotation;

		Vector3 camPos = _camera.Position;
		camPos.Z = Mathf.Lerp(camPos.Z, _zoomTarget, 0.1f);
		_camera.Position = camPos;
	}
}
