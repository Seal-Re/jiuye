# SteeringController.gd — AI 移动自然化 (mv-008)
# Wander Jitter: FastNoiseLite 2D 噪音 → 速度向量偏移 → 蛇形波浪线轨迹
# Separation: 多 AI 检测距离 → 反向推力 → 自然散开人群感
#
# 纯 View 层——不改 Core 决策、不碰 B.2/B.3

extends Node
class_name SteeringController

# —— FastNoiseLite 实例 ——
var _noise: FastNoiseLite
var _char_id_offset: float = 0.0

# —— 参数 ——
@export var wander_strength: float = 0.15     # 蛇形抖动强度 (0-1)
@export var wander_frequency: float = 1.0     # 抖动频率
@export var separation_radius: float = 30.0   # 排斥检测半径 (px)
@export var separation_strength: float = 0.4  # 排斥力强度 (0-1)
@export var max_separation_force: float = 60.0 # 排斥力上限


func _ready() -> void:
	_noise = FastNoiseLite.new()
	_noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	_noise.frequency = 0.05


# ================================================================
#  Wander Jitter: 基于时间+角色ID 的 2D 噪音偏移
#  返回 Vector2 叠加到 velocity 上 → 轨迹呈平滑蛇形
# ================================================================
func get_wander(delta: float, speed: float) -> Vector2:
	var t := Time.get_ticks_msec() * 0.001 * wander_frequency
	var nx := _noise.get_noise_2d(t, _char_id_offset)
	var ny := _noise.get_noise_2d(t + 100.0, _char_id_offset + 100.0)
	return Vector2(nx, ny) * wander_strength * speed


# ================================================================
#  Separation: 检测周围 Character2D → 距离<阈值 → 反向推力
#  推力与距离成反比，越近推力越大，有上限防止弹飞
# ================================================================
func get_separation(self_char: Character2D, all_chars: Array[Character2D]) -> Vector2:
	var force := Vector2.ZERO

	for other in all_chars:
		if other == self_char or not is_instance_valid(other):
			continue
		var dist := self_char.global_position.distance_to(other.global_position)
		if dist < separation_radius and dist > 0.01:
			# 推力 = 归一化方向 * (1 - dist/radius) * strength
			var push_dir := (self_char.global_position - other.global_position).normalized()
			var push_mag := (1.0 - dist / separation_radius) * separation_strength
			force += push_dir * push_mag * max_separation_force

	return force.limit_length(max_separation_force)


# ================================================================
#  为特定角色设置唯一噪音偏移 (基于 char_id)
# ================================================================
func set_char_offset(char_id: int) -> void:
	_char_id_offset = float(char_id) * 137.0  # 质数乘法确保不同角色噪音不相关
