# MapGenerator.gd — 四层叠加确定性瓦片地图生成器
# mv-002: WangHash 变体 + TileMapLayer×4 + 管线顺序（条件拦截先于装饰判定）
#
# 用法: 挂到 Main 节点，设置 tile_set 和 4 个 TileMapLayer 引用即可。

extends Node

# —— 配置 ——
@export var map_seed: int = 42
@export var chunk_size: int = 16
@export var grid_cols: int = 10   # 对应 C# WorldMap Node 网格
@export var grid_rows: int = 10

# —— 4 独立 TileMapLayer (Godot 4.3+ 强制) ——
@export var layer0_terrain: TileMapLayer   # MapLayer0_Terrain
@export var layer1_decor: TileMapLayer     # MapLayer1_Decor
@export var layer2_rare: TileMapLayer      # MapLayer2_Rare
@export var layer3_landmark: TileMapLayer  # MapLayer3_Landmark

# —— TileSet Source ID 映射 (由 mv-001 素材管线填充，当前先用纯色占位) ——
# Layer 0 变体映射: terrain_variant_id → Vector3i(source_id, atlas_x, atlas_y)
var _tile_map: Dictionary = {}

# —— 地标坐标 (Layer 3，从 C# WorldMap LandmarkDef 同步) ——
var _landmarks: Array[Vector2i] = []


# ================================================================
#  确定性哈希 (Wang Hash 变体 — 跨平台/跨版本稳定)
#  不依赖 Godot 内置 hash()，使用 & 0x7FFFFFFF 取正
# ================================================================
func hash_cell(x: int, y: int, p_seed: int, layer: int) -> int:
	var h: int = (x * 73856093) ^ (y * 19349663) ^ (p_seed * 83492791) ^ (layer * 631)
	h ^= h >> 16
	h *= 0x85ebca6b
	h ^= h >> 13
	h *= 0xc2b2ae35
	h ^= h >> 16
	return h & 0x7FFFFFFF


# ================================================================
#  区块生成入口
# ================================================================
func generate_full_map() -> void:
	for x in range(grid_cols):
		for y in range(grid_rows):
			_generate_cell(x, y)


func generate_chunk(chunk_pos: Vector2i, size: int) -> void:
	for dx in range(size):
		for dy in range(size):
			_generate_cell(chunk_pos.x + dx, chunk_pos.y + dy)


# ================================================================
#  单 cell 四层生成 —— 管线顺序铁律:
#   1. Layer0 基础哈希变体
#   2. 条件拦截 (T08-T11 覆盖)
#   3. 基于覆盖后 Layer0 → Layer1 装饰判定
#   4. 基于覆盖后 Layer0 → Layer2 稀有判定
#   5. Layer3 地标 (非概率，坐标精确匹配)
# ================================================================
func _generate_cell(x: int, y: int) -> void:
	# —— 1. Layer 0: 基础地形变体 ——
	var h0 = hash_cell(x, y, map_seed, 0)
	var roll = h0 % 100
	var terrain_id: int   # 最终 Layer 0 变体 ID (T01-A=0, T01-B=1, ...)

	# 从 C# 侧获取该 NodeId 的 Region.Peril/QiDensity/Element/TerrainKind
	# (当前用占位: 假设全部为平原 T01。等 C#→GDScript 桥接完成后再替换)
	var peril: int = 0
	var qi_density: int = 50
	var element: int = 0
	var terrain_kind: int = 0

	if terrain_kind == 0:  # Plain 平原 T01
		if roll < 60: terrain_id = 0      # T01-A 草地·翠绿
		elif roll < 85: terrain_id = 1    # T01-B 草地·茂密
		else: terrain_id = 2              # T01-C 草地·枯黄
	elif terrain_kind == 1:  # Sea 海域 T02
		if roll < 60: terrain_id = 3      # T02-A
		elif roll < 85: terrain_id = 4
		else: terrain_id = 5
	elif terrain_kind == 2:  # Desert 荒漠 T03
		if roll < 60: terrain_id = 6
		elif roll < 85: terrain_id = 7
		else: terrain_id = 8
	elif terrain_kind == 3:  # MountainFire T04
		if roll < 60: terrain_id = 9
		elif roll < 85: terrain_id = 10
		else: terrain_id = 11
	elif terrain_kind == 4:  # Jungle T05
		if roll < 60: terrain_id = 12
		elif roll < 85: terrain_id = 13
		else: terrain_id = 14
	elif terrain_kind == 5:  # MountainForest T06
		if roll < 60: terrain_id = 15
		elif roll < 85: terrain_id = 16
		else: terrain_id = 17
	else:  # Marsh T07
		if roll < 60: terrain_id = 18
		elif roll < 85: terrain_id = 19
		else: terrain_id = 20

	# —— 2. 条件拦截: 特殊地形 T08-T11 ——
	if peril >= 80 and element == 3:  # Fire + Peril≥80 → Volcano
		terrain_id = 21  # T08 火山
	elif peril >= 70 and terrain_kind == 2:  # Desert + Peril≥70 → Snowfield
		terrain_id = 22  # T09 雪原
	elif qi_density <= 25 and peril >= 70:
		terrain_id = 23  # T10 鬼域
	elif qi_density >= 70:
		# T11 灵脉福地 — 还需 Wealth≥70
		terrain_id = 24  # T11 (实际判定需 Wealth 字段)

	# 写入 Layer 0
	_write_tile(layer0_terrain, x, y, terrain_id)

	# —— 3. Layer 1: 装饰物 (<15% 覆盖率) ——
	var h1 = hash_cell(x, y, map_seed, 1)
	if h1 % 1000 < 150:  # 15% 全局覆盖率
		var decor_id = _pick_decoration(terrain_id, h1)
		if decor_id >= 0:
			_write_tile(layer1_decor, x, y, decor_id)

	# —— 4. Layer 2: 稀有特征 (<3% 覆盖率) ——
	var h2 = hash_cell(x, y, map_seed, 2)
	if h2 % 1000 < 30:  # 3% 全局覆盖率
		var rare_id = _pick_rare(terrain_id, h2)
		if rare_id >= 0:
			_write_tile(layer2_rare, x, y, rare_id)

	# —— 5. Layer 3: 地标建筑 (精确匹配，非概率) ——
	for lm in _landmarks:
		if lm.x == x and lm.y == y:
			_write_tile(layer3_landmark, x, y, lm.z)  # z = landmark tile ID


# ================================================================
#  装饰物选择 (Layer 1) — 匹配最终 Layer 0 变体
#  覆盖率 = hash_cell % 1000 < 各自的阈值
# ================================================================
func _pick_decoration(terrain_id: int, h: int) -> int:
	var r = h % 1000
	match terrain_id:
		0:  # T01-A 草地·翠绿
			if r < 120: return 30   # D01 野花丛
		2:  # T01-C 草地·枯黄
			if r < 100: return 32   # D03 灌木
		6:  # T03-A 沙地·金黄
			if r < 80: return 36    # D07 仙人掌
		7:  # T03-B 沙地·碎石
			if r < 50: return 37    # D08 枯骨
		12: # T05-A 密林·暗绿
			if r < 100: return 41   # D12 毒蘑菇
		18: # T07-A 水面·青绿
			if r < 100: return 47   # D18 浮萍
		19: # T07-B 水面·莲花
			if r < 80: return 48    # D19 蜻蜓
	return -1  # 不放置


# ================================================================
#  稀有特征选择 (Layer 2) — 匹配最终 Layer 0 变体
#  覆盖率 = hash_cell % 1000 < 各自的阈值，且部分触发叙事事件
# ================================================================
func _pick_rare(terrain_id: int, h: int) -> int:
	var r = h % 1000
	match terrain_id:
		1:  # T01-B 草地·茂密
			if r < 10: return 60    # R01 宝箱
		5:  # T02-C 海面·暗涌
			if r < 20: return 61    # R02 沉船
		8:  # T03-C 沙地·龟裂
			if r < 15: return 62    # R03 上古石碑
		11: # T04-C 岩壁·熔隙
			if r < 20: return 63    # R04 地火喷口
		14: # T05-C 密林·瘴紫
			if r < 20: return 64    # R05 妖兽巢穴
		17: # T06-C 山峦·雾霭
			if r < 10: return 65    # R06 隐士草庐
		20: # T07-C 水面·芦苇
			if r < 15: return 66    # R07 废弃渡口
		24: # T11 灵脉福地
			if r < 20: return 68    # R09 灵泉
		23: # T10 鬼域
			if r < 15: return 69    # R10 劫烬余痕
	return -1


# ================================================================
#  瓦片写入 — set_cell(coords, source_id, atlas_coords)
#  当前用纯色矩形占位 (source_id=0, atlas_coords=terrain_id)
#  素材管线 (mv-001) 完成后替换为真实 TileSet Atlas
# ================================================================
func _write_tile(layer: TileMapLayer, x: int, y: int, tile_id: int) -> void:
	if layer == null:
		return
	var coords := Vector2i(x, y)
	if _tile_map.has(tile_id):
		var info: Vector3i = _tile_map[tile_id]
		layer.set_cell(coords, info.x, Vector2i(info.y, info.z))
	else:
		# 占位: 用不同的纯色 source_id 区分 terrain_id (后续替换)
		# source_id 0 = 占位图集, atlas_y = terrain_id % 8, atlas_x = terrain_id / 8
		layer.set_cell(coords, 0, Vector2i(tile_id % 8, tile_id / 8))


# ================================================================
#  C# 桥接接口 (后续 story: WorldBridge→MapGenerator 数据注入)
#  当前用占位默认值; 桥接完成后传入真实 RegionDef/NodeGeo 数据
# ================================================================
func set_region_data(data: Array) -> void:
	# data[i] = {peril, qi_density, element, terrain_kind}
	# 后续实现
	pass


func set_landmarks(coords: Array[Vector3i]) -> void:
	_landmarks = coords  # Vector3i(x, y, landmark_tile_id)
