using Godot;

public partial class GameCamera : Node3D
{
	[ExportCategory("Pan/Rotate/Zoom (Gameplay)")]
	[Export] public float GameplayPanSpeed = 0.2f;           // ← gameplay-only
	[Export] public float PanSpeedBoost = 2.0f;
	[Export] public float RotateSpeed = 1.2f;
	[Export] public float ZoomSpeed = 2.0f;
	[Export(PropertyHint.Range, "0.01,0.4")] public float Smoothness = 0.1f;
	[Export] public float MinZoom = 0.0f;
	[Export] public float MaxZoom = 40.0f;
	[Export] public float MouseSensitivity = 0.2f;
	[Export] public float EdgeSize = 3.0f;
	[Export] public Camera3D Camera { get; private set; }

	[ExportCategory("Cinematic")]
	[Export] public bool CinematicEnabled = true;
	[Export(PropertyHint.Range, "0.01,0.8")] public float CinematicSmoothness = 0.18f;
	[Export(PropertyHint.Range, "-80.0,-5.0,0.5")] public float CinematicPitchMin = -60f;
	[Export(PropertyHint.Range, "-80.0,-5.0,0.5")] public float CinematicPitchMax = -10f;
	[Export(PropertyHint.Range, "0.05,1.5,0.01")] public float CinematicMouseYawSensitivity = 0.25f;
	[Export(PropertyHint.Range, "0.05,1.5,0.01")] public float CinematicMousePitchSensitivity = 0.25f;

	// Ctrl + Wheel changes ONLY this one (not gameplay)
	[ExportCategory("Cinematic Pan Speed Tuning")]
	[Export(PropertyHint.Range, "0.01,40.0,0.01")] public float CinematicPanSpeed = 0.25f;
	[Export(PropertyHint.Range, "0.01,40.0,0.01")] public float CinematicPanSpeedMin = 0.05f;
	[Export(PropertyHint.Range, "0.01,40.0,0.01")] public float CinematicPanSpeedMax = 8.0f;
	[Export(PropertyHint.Range, "1.02,2.00,0.01")] public float PanSpeedWheelFactor = 1.15f; // >1.0

	private Vector2 _mapSize;
	private Node3D _zoomPivot;
	private Vector3 _moveTarget;
	private float _rotateKeysTarget = 0.0f; // yaw target (deg)
	private float _zoomTarget = 0.0f;       // Camera local Z
	private GlobalResources _globalResources;

	private bool _cinematic;
	private Vector3 _savedMoveTarget;
	private float _savedRotateTarget;
	private float _savedZoomTarget;
	private float _savedPitchDeg;
	private float _savedGameplayPanSpeed;   // ← restore gameplay speed after cinematic

	private float _pitchTargetDeg;

	public override void _Ready()
	{
		Utils.NullExportCheck(Camera);

		_globalResources = GlobalResources.Instance;
		_zoomPivot = GetNode<Node3D>("CameraZoomPivot");
		_mapSize = _globalResources.MapSize;

		_moveTarget = Position;
		_rotateKeysTarget = RotationDegrees.Y;
		_zoomTarget = Camera.Position.Z;
		_pitchTargetDeg = _zoomPivot.RotationDegrees.X;
	}

	public override void _Input(InputEvent e)
	{
		// Alt + C toggles cinematic
		if (e is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.C && k.AltPressed && CinematicEnabled)
		{
			ToggleCinematic();
			return;
		}

		// Cinematic: Ctrl + wheel changes *only* cinematic pan speed (WheelUp=faster, WheelDown=slower)
		if (_cinematic && e is InputEventMouseButton mb && mb.Pressed &&
			(mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown))
		{
			bool ctrl = mb.CtrlPressed;
			if (ctrl)
			{
				if (mb.ButtonIndex == MouseButton.WheelUp)
					CinematicPanSpeed = Mathf.Clamp(CinematicPanSpeed * PanSpeedWheelFactor, CinematicPanSpeedMin, CinematicPanSpeedMax);
				else // WheelDown
					CinematicPanSpeed = Mathf.Clamp(CinematicPanSpeed / PanSpeedWheelFactor, CinematicPanSpeedMin, CinematicPanSpeedMax);
			}
			else
			{
				_zoomTarget = Mathf.Clamp(_zoomTarget + (mb.ButtonIndex == MouseButton.WheelDown ? +ZoomSpeed : -ZoomSpeed), MinZoom, MaxZoom);
			}
			return;
		}

		// Cinematic: MMB drag to adjust yaw/pitch
		if (_cinematic && e is InputEventMouseMotion mmm && Input.IsMouseButtonPressed(MouseButton.Middle))
		{
			_rotateKeysTarget -= (float)(mmm.Relative.X * CinematicMouseYawSensitivity);
			_pitchTargetDeg -= (float)(mmm.Relative.Y * CinematicMousePitchSensitivity);
			_pitchTargetDeg = Mathf.Clamp(_pitchTargetDeg, CinematicPitchMin, CinematicPitchMax);
			return;
		}

		// Original rotate action (e.g., RMB drag)
		if (e is InputEventMouseMotion motionEvent && Input.IsActionPressed("rotate"))
			_rotateKeysTarget -= (float)(motionEvent.Relative.X * MouseSensitivity);

		HideMouseIfRotating(e);
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
		ClampMoveTarget();
	}

	private void ToggleCinematic()
	{
		_cinematic = !_cinematic;

		if (_cinematic)
		{
			_savedMoveTarget = _moveTarget;
			_savedRotateTarget = _rotateKeysTarget;
			_savedZoomTarget = _zoomTarget;
			_savedPitchDeg = _zoomPivot.RotationDegrees.X;
			_savedGameplayPanSpeed = GameplayPanSpeed; // ← snapshot gameplay speed

			_pitchTargetDeg = _zoomPivot.RotationDegrees.X;
			Input.MouseMode = Input.MouseModeEnum.Hidden;
		}
		else
		{
			_moveTarget = _savedMoveTarget;
			_rotateKeysTarget = _savedRotateTarget;
			_zoomTarget = Mathf.Clamp(_savedZoomTarget, MinZoom, MaxZoom);

			var pivotRot = _zoomPivot.RotationDegrees;
			pivotRot.X = _savedPitchDeg;
			_zoomPivot.RotationDegrees = pivotRot;

			// Restore gameplay pan speed exactly
			GameplayPanSpeed = _savedGameplayPanSpeed;

			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	private void HideMouseIfRotating(InputEvent e)
	{
		if (Input.IsActionJustPressed("rotate"))
			Input.MouseMode = Input.MouseModeEnum.Captured;
		if (Input.IsActionJustReleased("rotate") && !_cinematic)
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private float CurrentPanSpeed()
	{
		return _cinematic ? CinematicPanSpeed : GameplayPanSpeed;
	}

	private void MouseEdgeScroll(double delta)
	{
		var viewport = GetViewport();
		Vector2 mousePos = viewport.GetMousePosition();
		Vector2 viewportSize = viewport.GetVisibleRect().Size;

		if (mousePos.X < 0 || mousePos.X > viewportSize.X || mousePos.Y < 0 || mousePos.Y > viewportSize.Y)
			return;

		Vector3 dir = Vector3.Zero;
		if (mousePos.X < EdgeSize) dir.X = -1;
		else if (mousePos.X > viewportSize.X - EdgeSize) dir.X = 1;
		if (mousePos.Y < EdgeSize) dir.Z = -1;
		else if (mousePos.Y > viewportSize.Y - EdgeSize) dir.Z = 1;

		_moveTarget += Transform.Basis * dir * CurrentPanSpeed() * (float)delta;
		ClampMoveTarget();
	}

	private void KeyboardScroll(double delta)
	{
		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
		Vector3 moveDir = Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y);

		float rotateKeys = Input.GetAxis("rotate_left", "rotate_right");

		// In cinematic, block zoom when Ctrl held (keyboard path too)
		bool ctrlHeld = _cinematic && Input.IsKeyPressed(Key.Ctrl);
		int zoomDirection = ctrlHeld ? 0 :
			(Input.IsActionJustReleased("camera_zoom_out") ? 1 : 0) -
			(Input.IsActionJustReleased("camera_zoom_in") ? 1 : 0);

		float panBoost = Input.IsActionPressed("pan_speed_boost") ? PanSpeedBoost : 1.0f;

		_moveTarget += moveDir * CurrentPanSpeed() * panBoost * (float)delta;
		_rotateKeysTarget += rotateKeys * RotateSpeed * (float)delta;

		if (!_globalResources.IsPlacingStructure)
			_zoomTarget = Mathf.Clamp(_zoomTarget + zoomDirection * ZoomSpeed, MinZoom, MaxZoom);

		ClampMoveTarget();
	}

	private void UpdateCameraPosition()
	{
		float s = _cinematic ? CinematicSmoothness : Smoothness;

		// Smooth movement
		Position = Position.Lerp(_moveTarget, s);

		// Smooth yaw
		Vector3 rot = RotationDegrees;
		rot.Y = Mathf.Lerp(rot.Y, _rotateKeysTarget, s);
		RotationDegrees = rot;

		// Smooth zoom
		Vector3 camPos = Camera.Position;
		camPos.Z = Mathf.Lerp(camPos.Z, _zoomTarget, _cinematic ? CinematicSmoothness : 0.1f);
		Camera.Position = camPos;

		// Smooth pitch only in cinematic
		if (_cinematic)
		{
			var pivotRot = _zoomPivot.RotationDegrees;
			pivotRot.X = Mathf.Lerp(pivotRot.X, _pitchTargetDeg, CinematicSmoothness);
			_zoomPivot.RotationDegrees = pivotRot;
		}
	}

	private void ClampMoveTarget()
	{
		_moveTarget.X = Mathf.Clamp(_moveTarget.X, -_mapSize.X / 2, _mapSize.X / 2);
		_moveTarget.Z = Mathf.Clamp(_moveTarget.Z, -_mapSize.Y / 2, _mapSize.Y / 2);
	}
}
