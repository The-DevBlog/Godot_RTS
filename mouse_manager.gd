extends Node3D

@onready var camera = $"../Camera/CameraPosition/CameraRotationX/CameraZoomPivot/Camera3D"

var drag_active: bool = false

func _unhandled_input(event: InputEvent) -> void:
	if event is not InputEventMouseButton:
		return

	if Input.is_action_just_pressed("mb_primary"):
		var drag_start = get_mouse_world_position(camera)
		if drag_start != Vector3.ZERO:
			print("Drag start: ", drag_start)

	if Input.is_action_just_released("mb_primary"):
		var drag_end = get_mouse_world_position(camera)
		if drag_end != Vector3.ZERO:
			print("Drag end: ", drag_end)

func get_mouse_world_position(cam: Camera3D) -> Vector3:
	var mouse_pos = get_viewport().get_mouse_position()
	var from = cam.project_ray_origin(mouse_pos)
	var to = from + cam.project_ray_normal(mouse_pos) * 1000.0

	var space_state = cam.get_world_3d().direct_space_state
	var query = PhysicsRayQueryParameters3D.create(from, to)
	query.collide_with_areas = false
	query.collide_with_bodies = true

	var result = space_state.intersect_ray(query)
	if result:
		return result.position
	else:
		return Vector3.ZERO
