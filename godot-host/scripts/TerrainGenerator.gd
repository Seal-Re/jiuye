# TerrainGenerator.gd — FastNoiseLite 2D 地形生成器 (pcg-001)
# 多 Octave 分形噪声 → 高度图 + 湿度 + 温度 + 危险度
# 确定性: 固定 seed + FastNoiseLite 同 seed = 同输出
#
# ADR-0006 备选 B: View 生成 → 量化 → Core 快照

extends Node
class_name TerrainGenerator

# —— 噪声实例 ——
var _height_noise: FastNoiseLite
var _moisture_noise: FastNoiseLite
var _temperature_noise: FastNoiseLite
var _peril_noise: FastNoiseLite

# —— 参数 ——
@export var map_seed: int = 42
@export var map_width: int = 100     # 噪声采样网格 (独立于 NodeId)
@export var map_height: int = 100
@export var octaves: int = 4
@export var persistence: float = 0.5
@export var lacunarity: float = 2.0
@export var noise_scale: float = 0.03  # 频率缩放


func _ready() -> void:
	_setup_noise(_height_noise, map_seed)
	_setup_noise(_moisture_noise, map_seed + 1000)
	_setup_noise(_temperature_noise, map_seed + 2000)
	_setup_noise(_peril_noise, map_seed + 3000)


func _setup_noise(noise: FastNoiseLite, seed: int) -> void:
	noise = FastNoiseLite.new()
	noise.seed = seed
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.fractal_type = FastNoiseLite.FRACTAL_FBM
	noise.fractal_octaves = octaves
	noise.fractal_gain = persistence
	noise.fractal_lacunarity = lacunarity
	noise.frequency = noise_scale


# ================================================================
#  采样: 世界坐标 → 归一化噪声值 [0, 1]
# ================================================================
func sample_height(x: float, y: float) -> float:
	return (_height_noise.get_noise_2d(x, y) + 1.0) * 0.5


func sample_moisture(x: float, y: float) -> float:
	return (_moisture_noise.get_noise_2d(x, y) + 1.0) * 0.5


func sample_temperature(x: float, y: float) -> float:
	return (_temperature_noise.get_noise_2d(x, y) + 1.0) * 0.5


func sample_peril(x: float, y: float) -> float:
	return (_peril_noise.get_noise_2d(x * 0.7, y * 0.7) + 1.0) * 0.5


# ================================================================
#  生成全图: 返回 Dictionary[Vector2i, TerrainCell]
#  TerrainCell = {terrain_id, element, peril, qi_density}
# ================================================================
func generate_full_map() -> Dictionary:
	var result: Dictionary = {}
	for x in range(map_width):
		for y in range(map_height):
			var h := sample_height(x, y)
			var m := sample_moisture(x, y)
			var t := sample_temperature(x, y)
			var p := sample_peril(x, y)
			result[Vector2i(x, y)] = _classify_cell(h, m, t, p)
	return result


# ================================================================
#  Whittaker 生物群系分类 (简化)
#  输入: height[0-1], moisture[0-1], temp[0-1], peril[0-1]
#  输出: {terrain_id (T01-T11), element, peril[0-100], qi_density[0-100]}
# ================================================================
func _classify_cell(h: float, m: float, t: float, p: float) -> Dictionary:
	var terrain_id: int
	var element: int   # ElementKind
	var qi := int(clamp(h * 80 + m * 40, 0, 100))

	# 极端危险度优先
	if p > 0.85:
		if t > 0.6:
			return {"terrain": 21, "element": 3, "peril": int(p * 100), "qi": qi}  # T08 火山
		else:
			return {"terrain": 23, "element": 5, "peril": int(p * 100), "qi": qi}  # T10 鬼域
	if qi >= 70 and p < 0.3:
		return {"terrain": 24, "element": 5, "peril": 10, "qi": qi}  # T11 灵脉

	# 极寒
	if t < 0.2 and h > 0.5:
		return {"terrain": 22, "element": 2, "peril": 60, "qi": qi}  # T09 雪原

	# 高温+低湿 → 荒漠
	if t > 0.6 and m < 0.35:
		return {"terrain": 6, "element": 2, "peril": int(p * 60), "qi": qi}  # T03-A 荒漠

	# 高温+高湿 → 林莽
	if t > 0.55 and m > 0.5:
		return {"terrain": 12, "element": 4, "peril": int(p * 80), "qi": qi}  # T05-A 林莽

	# 低温+高湿 → 山峦密林
	if t < 0.4 and m > 0.5 and h > 0.4:
		return {"terrain": 15, "element": 4, "peril": int(p * 60), "qi": qi}  # T06-A 山峦

	# 极高湿+低海拔 → 水泽
	if m > 0.7 and h < 0.4:
		return {"terrain": 18, "element": 1, "peril": int(p * 40), "qi": qi}  # T07-A 水泽

	# 极高海拔 → 山岳火
	if h > 0.75:
		return {"terrain": 9, "element": 3, "peril": int(p * 70), "qi": qi}  # T04-A 山岳

	# 中海拔+高湿 → 山峦
	if h > 0.5 and m > 0.4:
		return {"terrain": 15, "element": 4, "peril": int(p * 50), "qi": qi}  # T06-A

	# 默认: 平原
	return {"terrain": 0, "element": 0, "peril": int(p * 30), "qi": qi}  # T01-A 平原


# ================================================================
#  按 NodeId 坐标采样 (pcg-002: 提供给 WorldBridge 快照)
#  node_positions: Array[Vector2] — NodeId → 世界坐标映射
#  返回: Array[Dictionary] — 每个 NodeId 的 TerrainSnapshot
# ================================================================
func sample_at_nodes(node_positions: Array[Vector2]) -> Array:
	var snapshots: Array = []
	for pos in node_positions:
		snapshots.append(_classify_cell(
			sample_height(pos.x, pos.y),
			sample_moisture(pos.x, pos.y),
			sample_temperature(pos.x, pos.y),
			sample_peril(pos.x, pos.y)))
	return snapshots
