# PathFinder.gd — AStarGrid2D 寻路 + 路点队列管理 (mv-007)
# 基于 TileMap 网格构建 A* 图，排除碰撞地形。
# 输出世界坐标路点队列 → 喂给 Character2D.path[]

extends Node

# —— 引用 ——
@export var tilemap_terrain: TileMapLayer   # Layer0 地形 (读碰撞)
@export var tilemap_walls: TileMapLayer     # Layer3 地标 (读碰撞)

# —— AStarGrid2D 实例 ——
var astar: AStarGrid2D
var grid_region: Rect2i    # 网格覆盖范围

# —— 不可通行 cell 缓存 (hash set of Vector2i) ——
var _blocked_cells: Dictionary = {}  # Vector2i → true


func _ready() -> void:
	astar = AStarGrid2D.new()
	astar.diagonal_mode = AStarGrid2D.DIAGONAL_MODE_NEVER  # 四方向移动
	astar.cell_size = Vector2i(48 + 4, 48 + 4)  # TileSize + Gap (对齐 WorldView)


# ================================================================
#  初始化: 基于 TileMap 网格范围构建 A* 图
#  排除所有不可通行 cell
# ================================================================
func initialize(grid_size: Vector2i) -> void:
	grid_region = Rect2i(Vector2i.ZERO, grid_size)
	astar.region = grid_region
	astar.update()

	# 遍历全图，标记不可通行 cell
	for x in range(grid_size.x):
		for y in range(grid_size.y):
			var cell := Vector2i(x, y)
			if _is_cell_blocked(cell):
				astar.set_point_solid(cell, true)
				_blocked_cells[cell] = true


# ================================================================
#  查询 cell 是否被阻挡
#  检查: 1) TileMap Physics Layer 碰撞  2) 地标建筑占据
# ================================================================
func _is_cell_blocked(cell: Vector2i) -> bool:
	# 检查 Layer0 地形碰撞 (TileSet Physics Layer 0)
	if tilemap_terrain != null:
		var tile_data := tilemap_terrain.get_cell_tile_data(cell)
		if tile_data != null and tile_data.get_collision_polygons_count(0) > 0:
			return true

	# 检查 Layer3 地标碰撞
	if tilemap_walls != null:
		var wall_data := tilemap_walls.get_cell_tile_data(cell)
		if wall_data != null and wall_data.get_collision_polygons_count(0) > 0:
			return true

	return false


# ================================================================
#  核心: 世界坐标 → A* 路径 → 世界坐标路点队列
#  返回 Array[Vector2] (空数组=不可达)
# ================================================================
func find_path(world_from: Vector2, world_to: Vector2, tilemap: TileMapLayer) -> Array[Vector2]:
	if tilemap == null:
		return []

	var start_cell := tilemap.local_to_map(world_from)
	var end_cell := tilemap.local_to_map(world_to)

	# 边界检查
	if not grid_region.has_point(start_cell) or not grid_region.has_point(end_cell):
		return []

	# 目标 cell 被阻挡 → 找到最近的可行走邻居
	if _blocked_cells.has(end_cell):
		end_cell = _find_nearest_walkable(end_cell)
		if end_cell == Vector2i.MAX:
			return []  # 无可行走邻居

	# A* 寻路
	var cell_path := astar.get_id_path(start_cell, end_cell)
	if cell_path.is_empty():
		return []

	# 转换为世界坐标中心点
	var world_path: Array[Vector2] = []
	for cell in cell_path:
		world_path.append(tilemap.map_to_local(cell) + Vector2(26, 26))  # tile 中心

	return world_path


# ================================================================
#  找到最近的可行走邻居 (BFS, 最大搜索半径 5)
# ================================================================
func _find_nearest_walkable(cell: Vector2i) -> Vector2i:
	for radius in range(1, 6):
		for dx in range(-radius, radius + 1):
			for dy in range(-radius, radius + 1):
				if abs(dx) + abs(dy) != radius:  # 只检查曼哈顿距离=radius的环
					continue
				var neighbor := cell + Vector2i(dx, dy)
				if grid_region.has_point(neighbor) and not _blocked_cells.has(neighbor):
					return neighbor
	return Vector2i.MAX


# ================================================================
#  动态更新 cell 通行状态 (门控解锁/飞行/境界突破时调用)
# ================================================================
func set_cell_passable(cell: Vector2i, passable: bool) -> void:
	if passable:
		astar.set_point_solid(cell, false)
		_blocked_cells.erase(cell)
	else:
		astar.set_point_solid(cell, true)
		_blocked_cells[cell] = true


# ================================================================
#  批量解锁区域 (门控通过/飞行时调用)
# ================================================================
func unlock_region(cells: Array[Vector2i]) -> void:
	for cell in cells:
		set_cell_passable(cell, true)


# ================================================================
#  为 Character2D 设置路径: 世界坐标目标 → A* → 注入 path 队列
# ================================================================
func navigate_to(character: Character2D, world_target: Vector2, node_id: int, tilemap: TileMapLayer) -> void:
	var world_path := find_path(character.global_position, world_target, tilemap)
	if world_path.is_empty():
		print("[PathFinder] 无法到达 NodeId=%d" % node_id)
		return

	character.path = world_path
	character.target_node_id = node_id
	character.is_interacting = false
