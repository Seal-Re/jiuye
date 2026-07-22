# ZoomLODController.gd — 视觉层级 LOD + 动态秘境 (mv-005)
# 缩放驱动三级切换: Region(大区) → Landmark(地标) → Site(节点)
# 秘境未显形: 半透明覆盖 + 问号标记
# QiDensity 可视化: 薄灵区偏暗 / 厚灵区灵气光点密度渐增
#
# 用法: 挂到 Camera2D 所在节点

extends Node

# —— 引用 ——
@export var camera: Camera2D
@export var tilemap_terrain: TileMapLayer
@export var tilemap_decor: TileMapLayer
@export var tilemap_rare: TileMapLayer
@export var tilemap_landmark: TileMapLayer

# —— 缩放阈值 ——
@export var zoom_region: float = 0.5      # <0.5x = Region 大区级
@export var zoom_landmark: float = 1.0    # 0.5-1.0x = Landmark 地标级
                                            # >1.0x = Site 节点级

# —— 状态 ——
enum LODLevel { REGION = 0, LANDMARK = 1, SITE = 2 }
var current_lod: LODLevel = LODLevel.REGION
var _secret_cells: Array[Vector2i] = []  # 秘境 cell 列表


func _ready() -> void:
	if camera == null:
		camera = get_viewport().get_camera_2d()


func _process(_delta: float) -> void:
	if camera == null:
		return

	var zoom := camera.zoom.x
	var new_lod: LODLevel

	if zoom < zoom_region:
		new_lod = LODLevel.REGION
	elif zoom < zoom_landmark:
		new_lod = LODLevel.LANDMARK
	else:
		new_lod = LODLevel.SITE

	if new_lod != current_lod:
		current_lod = new_lod
		_apply_lod()


func _apply_lod() -> void:
	match current_lod:
		LODLevel.REGION:
			# 大区级: 仅 Layer0 地形 + Region 边界线可见
			_set_layer_visible(tilemap_decor, false)
			_set_layer_visible(tilemap_rare, false)
			_set_layer_visible(tilemap_landmark, false)
		LODLevel.LANDMARK:
			# 地标级: Layer0 + 地标建筑可见
			_set_layer_visible(tilemap_decor, false)
			_set_layer_visible(tilemap_rare, false)
			_set_layer_visible(tilemap_landmark, true)
		LODLevel.SITE:
			# 节点级: 全部四层可见
			_set_layer_visible(tilemap_decor, true)
			_set_layer_visible(tilemap_rare, true)
			_set_layer_visible(tilemap_landmark, true)


func _set_layer_visible(layer: TileMapLayer, visible: bool) -> void:
	if layer != null:
		layer.visible = visible


# ================================================================
#  动态秘境: 未显形 = 半透明 + 问号标记
# ================================================================
func set_secret_cells(cells: Array[Vector2i]) -> void:
	_secret_cells = cells


func reveal_secret(cell: Vector2i) -> void:
	"""秘境被发现 → 移除覆盖层"""
	var idx := _secret_cells.find(cell)
	if idx >= 0:
		_secret_cells.remove_at(idx)
	# 刷新渲染 (触发重绘覆盖层)
	queue_redraw()


# ================================================================
#  QiDensity 可视化: 在 Camera2D 上叠加半透明光点层
#  薄灵区(Qi≤25): 偏暗灰覆盖 / 厚灵区(Qi≥56): 金色光点密度渐增
# ================================================================
func _draw() -> void:
	if camera == null:
		return

	# 秘境半透明覆盖
	for cell in _secret_cells:
		var world_pos := tilemap_terrain.map_to_local(cell) + Vector2i(26, 26)
		var screen_pos := camera.get_screen_center()  # 近似
		draw_rect(Rect2(world_pos - Vector2(22, 22), Vector2(44, 44)),
				  Color(0.2, 0.2, 0.2, 0.5))
		draw_string(ThemeDB.fallback_font, world_pos + Vector2(-6, -6),
					"?", HORIZONTAL_ALIGNMENT_CENTER, -1, 16,
					Color(1.0, 1.0, 0.5, 0.8))
