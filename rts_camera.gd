extends Node3D

#onready
@onready var rotation_x = $CameraRotationX
@onready var zoom_pivot = $CameraRotationX/CameraZoomPivot
@onready var camera = $CameraRotationX/CameraZoomPivot/Camera3D

#variables
var move_speed = 0.6
var move_target: Vector3
var rotate_keys_speed = 1.5
var rotate_keys_target: float
var zoom_speed = 3.0
var zoom_target: float
var min_zoom = -20.0
var max_zoom = 20.0
var mouse_sensitivity = 0.2
var edge_size = 5.0
var edge_scroll_speed = 0.6

func _ready() -> void:
	move_target = position
	rotate_keys_target = rotation_degrees.y
	zoom_target = camera.position.z


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion and Input.is_action_pressed("rotate"):
		rotate_keys_target -= event.relative.x * mouse_sensitivity
		rotation_x.rotation_degrees.x -= event.relative.y * mouse_sensitivity
		rotation_x.rotation_degrees.x = clamp(rotation_x.rotation_degrees.x, -10, 30)

func _process(_delta: float) -> void:
	# show/hide mouse button when rotating using mouse rotate
	if Input.is_action_just_pressed("rotate"):
		Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

	if Input.is_action_just_released("rotate"):
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)

	# edge scroll
	var mouse_pos = get_viewport().get_mouse_position()
	var viewport_size = get_viewport().get_visible_rect().size

	var scroll_direction = Vector3.ZERO
	if mouse_pos.x < edge_size:
		scroll_direction.x = -1
	elif mouse_pos.x > viewport_size.x - edge_size:
		scroll_direction.x = 1

	if mouse_pos.y < edge_size:
		scroll_direction.z = -1
	elif mouse_pos.y > viewport_size.y - edge_size:
		scroll_direction.z = 1

	move_target += transform.basis * scroll_direction * edge_scroll_speed

	# get input direction
	var input_direction = Input.get_vector("left", "right", "up", "down")
	var movement_direction = (transform.basis * Vector3(input_direction.x, 0, input_direction.y))
	var rotate_keys = Input.get_axis("rotate_left", "rotate_right")
	var zoom_direction = (int(Input.is_action_just_released("camera_zoom_out")) -
						  int(Input.is_action_just_released("camera_zoom_in")))

	# set movement target
	move_target += movement_direction * move_speed;
	rotate_keys_target += rotate_keys * rotate_keys_speed
	zoom_target += zoom_direction * zoom_speed;
	
	# lerp to new position 
	position = lerp(position, move_target, 0.05)
	rotation_degrees.y = lerp(rotation_degrees.y, rotate_keys_target, 0.05)
	camera.position.z = lerp(camera.position.z, zoom_target, 0.1)
