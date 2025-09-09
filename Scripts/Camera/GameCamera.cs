using Godot;

public partial class GameCamera : Node3D
{
	[ExportCategory("Pan/Rotate/Zoom (Gameplay)")]
	[Export] public float PanSpeed = 0.2f;
	[Export] public float PanSpeedBoost = 2.0f;
	[Export] public float RotateSpeed = 1.2f;
	[Export] public float ZoomSpeed = 2.0f;
	[Export(PropertyHint.Range, "0.01,0.4")] public float Smoothness = 0.1f;

	[ExportCategory("Shared Limits")]
	[Export] public float MinZoom = 0.0f;
	[Export] public float MaxZoom = 40.0f;
	[Export] public float MouseSensitivity = 0.2f;
	[Export] public float EdgeSize = 3.0f;

	[ExportCategory("Cinematic")]
	[Export(PropertyHint.Range, "0.01,0.4")] public float CinematicSmoothness = 0.12f;
	[Export] public float CinematicPanSpeed = 0.25f;
	[Export] public float CinematicZoomSpeed = 6.0f;            // units/sec while key held
	[Export] public float CinematicRotateSensitivity = 0.22f;   // MMB drag sensitivity
	[Export] public float CinematicPanSpeedStep = 2.0f;         // step size per “tick” while LMB/RMB held
	[Export] public float CinematicPanSpeedMin = 0.01f;
	[Export] public float CinematicPanSpeedMax = 10f;
	[Export] public float CinematicPitchMinDeg = -80f;
	[Export] public float CinematicPitchMaxDeg = -5f;

	[Export] public Camera3D Camera { get; private set; }

	private Vector2 _mapSize;
	private Node3D _zoomPivot; // child that contains Camera (pitch pivot)
	private Vector3 _moveTarget;
	private float _yawTargetDeg;
	private float _pitchTargetDeg; // cinematic pitch target
	private float _zoomTarget;

	private GlobalResources _globalResources;

	// Cinematic state
	private bool _cinematic = false;
	private float _savedGameplayZoom;
	private float _savedGameplayPitchDeg;

	// Pitch exit-lerp state
	private bool _lerpToGameplayPitch = false;
	private float _pitchLerpTargetDeg = 0f;

	// LMB/RMB hold-to-step timing
	private float _cinePanStepAccum = 0f;
	private const float _cinePanStepInterval = 0.10f;

	public override void _Ready()
	{
		Utils.NullExportCheck(Camera);

		_globalResources = GlobalResources.Instance;
		_zoomPivot = GetNode<Node3D>("CameraZoomPivot");
		_mapSize = _globalResources.MapSize;

		_moveTarget = Position;
		_yawTargetDeg = RotationDegrees.Y;
		_pitchTargetDeg = _zoomPivot.RotationDegrees.X; // keep current
		_zoomTarget = Camera.Position.Z;

		// keep pitch sane initially (for cinematic clamps)
		_pitchTargetDeg = Mathf.Clamp(_pitchTargetDeg, CinematicPitchMinDeg, CinematicPitchMaxDeg);
	}

	public override void _Input(InputEvent @event)
	{
		// Toggle cinematic mode (bind to Alt+C: "cinematic_camera_toggle")
		if (@event.IsActionPressed("cinematic_camera_toggle"))
		{
			ToggleCinematic();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_cinematic)
		{
			// MMB drag to rotate yaw/pitch
			if (@event is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Middle))
			{
				_yawTargetDeg -= motion.Relative.X * CinematicRotateSensitivity;
				_pitchTargetDeg -= motion.Relative.Y * CinematicRotateSensitivity;
				_pitchTargetDeg = Mathf.Clamp(_pitchTargetDeg, CinematicPitchMinDeg, CinematicPitchMaxDeg);
				GetViewport().SetInputAsHandled();
			}

			// Eat LMB/RMB so they don't click gameplay UI while in cinematic
			if (@event is InputEventMouseButton mb &&
				(mb.ButtonIndex == MouseButton.Left || mb.ButtonIndex == MouseButton.Right) &&
				mb.Pressed)
			{
				GetViewport().SetInputAsHandled();
			}
		}
		else
		{
			// Gameplay rotate while "rotate" is down
			if (@event is InputEventMouseMotion motion && Input.IsActionPressed("rotate"))
				_yawTargetDeg -= (float)(motion.Relative.X * MouseSensitivity);

			HideMouseIfRotating(@event);
		}
	}

	public override void _Process(double delta)
	{
		if (_cinematic) ApplyCinematicPanSpeedStepWhileHeld(delta);

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
			_savedGameplayZoom = _zoomTarget;
			_savedGameplayPitchDeg = _zoomPivot != null ? _zoomPivot.RotationDegrees.X : 0f;

			// enter cinematic from current gameplay pitch (clamped)
			_pitchTargetDeg = Mathf.Clamp(_savedGameplayPitchDeg, CinematicPitchMinDeg, CinematicPitchMaxDeg);

			_lerpToGameplayPitch = false; // cancel any pending exit lerp
			Input.MouseMode = Input.MouseModeEnum.Captured;
			_cinePanStepAccum = 0f;
		}
		else
		{
			_zoomTarget = _savedGameplayZoom; // snap back to gameplay height

			// start smooth pitch lerp back to saved gameplay pitch
			_pitchLerpTargetDeg = _savedGameplayPitchDeg;
			_lerpToGameplayPitch = true;

			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	private void HideMouseIfRotating(InputEvent @event)
	{
		if (Input.IsActionJustPressed("rotate"))
			Input.MouseMode = Input.MouseModeEnum.Captured;

		if (Input.IsActionJustReleased("rotate"))
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void MouseEdgeScroll(double delta)
	{
		var viewport = GetViewport();
		Vector2 mousePos = viewport.GetMousePosition();
		Vector2 viewportSize = viewport.GetVisibleRect().Size;
		Vector3 scrollDirection = Vector3.Zero;

		// only when mouse is inside the window
		if (mousePos.X < 0 || mousePos.X > viewportSize.X || mousePos.Y < 0 || mousePos.Y > viewportSize.Y)
			return;

		if (mousePos.X < EdgeSize) scrollDirection.X = -1;
		else if (mousePos.X > viewportSize.X - EdgeSize) scrollDirection.X = 1;

		if (mousePos.Y < EdgeSize) scrollDirection.Z = -1;
		else if (mousePos.Y > viewportSize.Y - EdgeSize) scrollDirection.Z = 1;

		float speed = CurrentPanSpeed();
		_moveTarget += Transform.Basis * scrollDirection * speed * (float)delta;
		ClampMoveTarget();
	}

	private void KeyboardScroll(double delta)
	{
		// WASD pan
		Vector2 inputDirection = Input.GetVector("left", "right", "up", "down");
		Vector3 movementDirection = Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y);

		// Shift boost ONLY in gameplay mode
		float panSpeedBoost = (!_cinematic && Input.IsActionPressed("pan_speed_boost")) ? PanSpeedBoost : 1.0f;

		_moveTarget += movementDirection * CurrentPanSpeed() * panSpeedBoost * (float)delta;

		// Yaw with keys
		float rotateKeys = Input.GetAxis("rotate_left", "rotate_right");
		_yawTargetDeg += rotateKeys * RotateSpeed * (float)delta;

		// Zoom
		if (_cinematic)
		{
			// Ctrl = zoom in (toward ground), Space = zoom out
			bool inHeld = Input.IsActionPressed("cinematic_camera_zoom_in");
			bool outHeld = Input.IsActionPressed("cinematic_camera_zoom_out");
			int dir = (outHeld ? 1 : 0) - (inHeld ? 1 : 0);
			if (dir != 0)
				_zoomTarget = Mathf.Clamp(_zoomTarget + dir * CinematicZoomSpeed * (float)delta, MinZoom, MaxZoom);
		}
		else
		{
			// Original wheel-released zoom
			int zoomDirection = (Input.IsActionJustReleased("camera_zoom_out") ? 1 : 0)
							  - (Input.IsActionJustReleased("camera_zoom_in") ? 1 : 0);

			if (!_globalResources.IsPlacingStructure && zoomDirection != 0)
				_zoomTarget = Mathf.Clamp(_zoomTarget + zoomDirection * ZoomSpeed, MinZoom, MaxZoom);
		}

		ClampMoveTarget();
	}

	private void UpdateCameraPosition()
	{
		float s = _cinematic ? CinematicSmoothness : Smoothness;

		// Smooth movement
		Position = Position.Lerp(_moveTarget, s);

		// Smooth yaw
		var rot = RotationDegrees;
		rot.Y = Mathf.Lerp(rot.Y, _yawTargetDeg, s);
		RotationDegrees = rot;

		// Smooth pitch
		if (_zoomPivot != null)
		{
			var p = _zoomPivot.RotationDegrees;

			if (_cinematic)
			{
				// cinematic uses its own pitch target
				p.X = Mathf.Lerp(p.X, _pitchTargetDeg, CinematicSmoothness);
			}
			else if (_lerpToGameplayPitch)
			{
				// lerp back to gameplay pitch after exiting cinematic
				p.X = Mathf.Lerp(p.X, _pitchLerpTargetDeg, Smoothness);

				// stop when close enough
				if (Mathf.Abs(p.X - _pitchLerpTargetDeg) < 0.05f)
				{
					p.X = _pitchLerpTargetDeg;
					_lerpToGameplayPitch = false;
				}
			}

			_zoomPivot.RotationDegrees = p;
		}

		// Smooth zoom
		var camPos = Camera.Position;
		camPos.Z = Mathf.Lerp(camPos.Z, _zoomTarget, s);
		Camera.Position = camPos;
	}

	private float CurrentPanSpeed() => _cinematic ? CinematicPanSpeed : PanSpeed;

	private void ClampMoveTarget()
	{
		_moveTarget.X = Mathf.Clamp(_moveTarget.X, -_mapSize.X / 2, _mapSize.X / 2);
		_moveTarget.Z = Mathf.Clamp(_moveTarget.Z, -_mapSize.Y / 2, _mapSize.Y / 2);
	}

	// --- Cinematic pan-speed step with LMB/RMB hold ---
	private void ApplyCinematicPanSpeedStepWhileHeld(double delta)
	{
		bool speedUp = Input.IsMouseButtonPressed(MouseButton.Right);
		bool slowDown = Input.IsMouseButtonPressed(MouseButton.Left);

		if (!speedUp && !slowDown)
		{
			_cinePanStepAccum = 0f;
			return;
		}

		_cinePanStepAccum += (float)delta;

		while (_cinePanStepAccum >= _cinePanStepInterval)
		{
			_cinePanStepAccum -= _cinePanStepInterval;

			if (speedUp)
				CinematicPanSpeed = Mathf.Clamp(CinematicPanSpeed + CinematicPanSpeedStep, CinematicPanSpeedMin, CinematicPanSpeedMax);
			if (slowDown)
				CinematicPanSpeed = Mathf.Clamp(CinematicPanSpeed - CinematicPanSpeedStep, CinematicPanSpeedMin, CinematicPanSpeedMax);
		}
	}
}
