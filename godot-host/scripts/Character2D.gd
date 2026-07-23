# Character2D.gd — 角色移动基础 (mv-006)
# 继承 CharacterBody2D，WASD 控制 + AI 路点跟随共用
#
# 速度受 Core 数据影响:
#   speed *= 1.0 / terrain_cost  (地形减速, 荒漠/水泽)
#   speed *= 1.0 + realm * 0.2    (境界加速, 高阶修士缩地成寸)
#   speed *= 0.8                  (水泽 T07 粘滞感)
#
# 特例解除: 筑基期/御剑飞行 → 临时禁用 T02/T04-T06 碰撞

extends CharacterBody2D
class_name Character2D

# —— 导出属性 ——
@export var speed: float = 150.0          # 基础速度 (px/s)
@export var is_player: bool = false       # 玩家=WASD控制, AI=路点跟随
@export var char_name: String = "无名"
@export var char_id: int = 0

# —— 移动状态 ——
var path: Array[Vector2] = []
var target_node_id: int = -1
var realm_level: int = 0
var terrain_speed_mult: float = 1.0
var is_moving: bool = false
var is_interacting: bool = false

# —— NPC 交互 (npc-001) ——
signal player_interact_requested(npc_char: Character2D)
var _nearby_player: Character2D = null
var _show_interact_prompt: bool = false

# —— Steering 噪声 (mv-008 接入) ——
var noise_offset: float = 0.0

# —— Area2D 互动检测 (30px) ——
@onready var interact_area: Area2D = $InteractArea


func _ready() -> void:
	# 创建互动检测区域
	var area := Area2D.new()
	area.name = "InteractArea"
	var shape := CollisionShape2D.new()
	shape.shape = CircleShape2D.new()
	shape.shape.radius = 30.0
	area.add_child(shape)
	add_child(area)
	interact_area = area


func _physics_process(delta: float) -> void:
	if is_interacting:
		return

	var effective_speed := speed * terrain_speed_mult * (1.0 + realm_level * 0.2)

	if is_player:
		_player_move(effective_speed)
	else:
		_ai_follow_path(effective_speed, delta)


# ================================================================
#  玩家 WASD 移动
# ================================================================
func _player_move(spd: float) -> void:
	var direction := Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	velocity = direction * spd
	move_and_slide()
	is_moving = direction.length() > 0.01


# ================================================================
#  AI 路点平滑跟随 + 中断检测
# ================================================================
func _ai_follow_path(spd: float, delta: float) -> void:
	if path.is_empty():
		velocity = Vector2.ZERO
		is_moving = false
		return

	is_moving = true
	var next_point := path[0]

	# 容差半径 5px → 切下一个路点
	if global_position.distance_to(next_point) < 5.0:
		path.pop_front()
		if path.is_empty():
			_on_arrive_at_target()
			return
		next_point = path[0]

	# 平滑转向目标点
	var direction := global_position.direction_to(next_point)
	velocity = direction * spd

	# Steering: Wander Jitter (FastNoiseLite Simplex)
	if not is_player and steering != null:
		velocity += steering.get_wander(delta, spd)

	# Steering: Separation (多 AI 距离检测)
	if not is_player and steering != null and all_chars_ref.size() > 0:
		velocity += steering.get_separation(self, all_chars_ref)

	move_and_slide()

	# 中断检测: 玩家靠近 30px → 清空 path → Interacting
	_check_interrupt()


# ================================================================
#  到达目的地 → Core CommandIntent 写回 (mv-007 实现)
# ================================================================
func _on_arrive_at_target() -> void:
	is_moving = false
	# TODO: 通过 WorldBridge 写回 CommandIntent(Travel, To=target_node_id)
	print("[Character2D] %s 到达 NodeId=%d" % [char_name, target_node_id])


func _check_interrupt() -> void:
	var bodies := interact_area.get_overlapping_bodies()
	for body in bodies:
		if body is Character2D and body.is_player and not is_player:
			# 玩家靠近 → AI 中断移动, 切换状态
			path.clear()
			is_interacting = true
			print("[Character2D] %s 被玩家中断，切入交互" % char_name)
			return


# ================================================================
#  地形效果: 从 tile_id 查询 TerrainData → 减速/灼烧/中毒
# ================================================================
func apply_terrain_effect(tile_id: int) -> void:
	var effect := TerrainData.get_effect(tile_id)
	if effect == TerrainData.EffectKind.NONE:
		terrain_speed_mult = 1.0
		return

	var data := TerrainData.get_effect_data(effect)
	terrain_speed_mult = data["speed"]

	# 持续伤害 (dmg>0 且 tick>0)
	if data["dmg"] > 0 and data["tick"] > 0:
		_start_dot_timer(data["dmg"], data["tick"], data["duration"])


func _start_dot_timer(dmg: int, interval: float, duration: float) -> void:
	var timer := Timer.new()
	timer.wait_time = interval
	timer.one_shot = false
	add_child(timer)
	timer.timeout.connect(_apply_dot.bind(dmg))
	timer.start()

	# 持续时间后停止
	var stop_timer := Timer.new()
	stop_timer.wait_time = duration
	stop_timer.one_shot = true
	add_child(stop_timer)
	stop_timer.timeout.connect(timer.queue_free)
	stop_timer.timeout.connect(stop_timer.queue_free)
	stop_timer.start()


func _apply_dot(dmg: int) -> void:
	# TODO: 玩家 HP -= dmg (需 HP 系统，当前占位)
	print("[Character2D] %s 受到 %d 点地形伤害" % [char_name, dmg])
func set_target(world_target: Vector2, node_id: int, tilemap: TileMapLayer) -> void:
	"""设置 A* 路点队列 (由 MapGenerator/AStarGrid2D 调用)"""
	target_node_id = node_id
	# 简化: 直接用直线路径 (AStarGrid2D 在 mv-007 接入)
	path = [world_target]
	is_interacting = false


func set_terrain_by_tile(tile_id: int) -> void:
	"""从 tile_id 查询 TerrainData 设置地形效果"""
	apply_terrain_effect(tile_id)


func set_realm_level(realm: int) -> void:
	"""境界加速"""
	realm_level = realm


func enable_flight() -> void:
	"""御剑飞行: 禁用地面碰撞层"""
	collision_layer = 0
	collision_mask = 0
	set_collision_layer_value(1, false)

# ================================================================
#  NPC 交互系统 (npc-001)
# ================================================================
func _process(_delta: float) -> void:
	if is_player:
		_check_nearby_npc_for_interact()
	elif _show_interact_prompt:
		_check_player_still_nearby()


func _check_nearby_npc_for_interact() -> void:
	if not Input.is_action_just_pressed("ui_accept"):  # E / Enter
		return
	var bodies := interact_area.get_overlapping_bodies()
	for body in bodies:
		if body is Character2D and not body.is_player:
			player_interact_requested.emit(body)
			is_interacting = true
			return


func _check_player_still_nearby() -> void:
	var bodies := interact_area.get_overlapping_bodies()
	_nearby_player = null
	for body in bodies:
		if body is Character2D and body.is_player:
			_nearby_player = body
			return
	_show_interact_prompt = false


func show_interact_prompt() -> void:
	_show_interact_prompt = true


func hide_interact_prompt() -> void:
	_show_interact_prompt = false


func set_npc_context(data: Dictionary) -> void:
	npc_data = data
