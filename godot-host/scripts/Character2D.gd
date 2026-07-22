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
var path: Array[Vector2] = []             # AI 路点队列 (世界坐标)
var target_node_id: int = -1              # 目标 Core NodeId
var realm_level: int = 0                  # 境界等级 (影响速度)
var terrain_speed_mult: float = 1.0       # 地形减速乘子
var is_moving: bool = false
var is_interacting: bool = false

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

	# Steering: Wander Jitter (mv-008 接入，当前用简单正弦)
	if not is_player:
		velocity += _wander_jitter(delta) * spd * 0.1

	# Steering: Separation (mv-008 接入，当前占位)
	velocity += _separation_force() * spd * 0.3

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


# ================================================================
#  Steering Behaviors (mv-008 完整实现，当前占位)
# ================================================================
func _wander_jitter(_delta: float) -> Vector2:
	return Vector2(sin(Time.get_ticks_msec() * 0.001 + noise_offset) * 0.5,
				   cos(Time.get_ticks_msec() * 0.0013 + noise_offset) * 0.5)


func _separation_force() -> Vector2:
	var force := Vector2.ZERO
	var neighbors := interact_area.get_overlapping_bodies()
	for body in neighbors:
		if body is Character2D and body != self:
			var dist := global_position.distance_to(body.global_position)
			if dist < 30.0 and dist > 0.01:
				force += (global_position - body.global_position).normalized() * (30.0 - dist) / 30.0
	return force


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
#  公共接口
# ================================================================
func set_target(world_target: Vector2, node_id: int, tilemap: TileMapLayer) -> void:
	"""设置 A* 路点队列 (由 MapGenerator/AStarGrid2D 调用)"""
	target_node_id = node_id
	# 简化: 直接用直线路径 (AStarGrid2D 在 mv-007 接入)
	path = [world_target]
	is_interacting = false


func set_terrain_speed(cost: float, on_water: bool) -> void:
	"""地形减速 (由 MapGenerator 查询 CollisionPassKind 后调用)"""
	terrain_speed_mult = 1.0 / max(cost, 0.1)
	if on_water:
		terrain_speed_mult *= 0.8


func set_realm_level(realm: int) -> void:
	"""境界加速"""
	realm_level = realm


func enable_flight() -> void:
	"""御剑飞行: 禁用地面碰撞层"""
	collision_layer = 0
	collision_mask = 0
	set_collision_layer_value(1, false)
