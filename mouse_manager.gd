extends Node3D

@onready var camera = $"../Camera/CameraPosition/CameraRotationX/CameraZoomPivot/Camera3D"

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.is_pressed() and event.button_index == MOUSE_BUTTON_LEFT:
		var ground_hit = get_mouse_ground_position(camera)
		if ground_hit != Vector3.ZERO:
			print("Hit ground at:", ground_hit)

func get_mouse_ground_position(cam: Camera3D) -> Vector3:
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
