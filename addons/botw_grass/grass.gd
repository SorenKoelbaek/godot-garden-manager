@tool
extends MultiMeshInstance3D

const MeshFactory = preload("res://addons/botw_grass/mesh_factory.gd")
const GrassFactory = preload("res://addons/botw_grass/grass_factory.gd")

@export var blade_width: Vector2 = Vector2(0.01, 0.02):
	set(new_value):
		blade_width = new_value
		rebuild()

@export var blade_height: Vector2 = Vector2(0.08, 0.15):
	set(new_value):
		blade_height = new_value
		rebuild()

@export var sway_yaw: Vector2 = Vector2(0.0, 10.0):
	set(new_value):
		sway_yaw = new_value
		rebuild()

@export var sway_pitch: Vector2 = Vector2(0.04, 0.08):
	set(new_value):
		sway_pitch = new_value
		rebuild()

@export var mesh: Mesh = null:
	set(new_value):
		mesh = new_value
		rebuild()

@export var density: float = 1.0:
	set(new_value):
		density = new_value
		if density < 1.0:
			density = 1.0
		rebuild()

func rebuild():
	var width: float = 1.0
	var depth: float = 1.0

	if is_inside_tree() and not Engine.is_editor_hint():
		var gp := get_parent()
		if gp:
			var garden := gp.get_parent()
			if garden and garden.has_method("get_Width") and garden.has_method("get_Depth"):
				width = float(garden.Width)
				depth = float(garden.Depth)

	if !multimesh:
		multimesh = MultiMesh.new()

	# Reset instance count before regeneration
	multimesh.instance_count = 0

	# Generate grass spawn data
	var spawns: Array = GrassFactory.generate(
		mesh,
		density,
		blade_width,
		blade_height,
		sway_pitch,
		sway_yaw
	)

	if spawns.is_empty():
		return

	# ---- NEW UV BOUNDING BOX CODE (FIRST PASS) ----
	var min_x := INF
	var max_x := -INF
	var min_z := INF
	var max_z := -INF

	for spawn in spawns:
		var pos: Vector3 = spawn[0].origin
		min_x = min(min_x, pos.x)
		max_x = max(max_x, pos.x)
		min_z = min(min_z, pos.z)
		max_z = max(max_z, pos.z)

	# ---- END BOUNDING BOX ----

	# Setup MultiMesh
	multimesh.mesh = MeshFactory.simple_grass()
	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.use_custom_data = true
	multimesh.use_colors = true
	multimesh.instance_count = spawns.size()

	# ---- SECOND PASS: APPLY UVs ----
	for index in multimesh.instance_count:
		var spawn: Array = spawns[index]
		multimesh.set_instance_transform(index, spawn[0])
		multimesh.set_instance_custom_data(index, spawn[1])

		var pos: Vector3 = spawn[0].origin
		var u: float = (pos.x - min_x) / (max_x - min_x)
		var v: float = (pos.z - min_z) / (max_z - min_z)

		multimesh.set_instance_color(index, Color(u, 0.0, v, 1.0))
