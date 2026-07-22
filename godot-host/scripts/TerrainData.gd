# TerrainData.gd — 碰撞通行枚举 + 地形效果系统 (mv-009)
# CollisionPassKind × 5 + TerrainEffect × 7
# 纯数据定义，供 MapGenerator + Character2D + PathFinder 共用

extends Node
class_name TerrainData


# ================================================================
#  CollisionPassKind — 通行类型枚举
# ================================================================
enum PassKind {
	GROUND = 0,           # 常规地面: 平原、荒漠、道路 (可通行)
	SHALLOW_WATER = 1,    # 浅水/水泽: 可通行但降速 (T07-A/C)
	WALL = 2,             # 普通障碍: 建筑、密林、山壁 (地面不可, 飞行可)
	DEEP_SEA_OR_CHASM = 3,# 深海/绝壁: 不可通行, 需御剑/行船/境界解除
	GATE_PASS = 4         # 关隘/境界门控: 需凭证或修为交互通过
}


# ================================================================
#  TerrainEffect — 地形效果枚举
# ================================================================
enum EffectKind {
	NONE = 0,
	SLOW = 1,              # 减速
	MUD = 2,               # 泥潭 (减速+下沉)
	BURN = 3,              # 灼烧
	MIASMA = 4,            # 瘴气 (中毒)
	FROST = 5,             # 冰冻
	SPIRIT_BLESSING = 6,   # 灵脉福地 (加速+回灵)
	SUPPRESS_OR_HEAVY = 7  # 禁空/重力
}


# ================================================================
#  TileType → PassKind 映射表 (瓦片 ID → 通行类型)
# ================================================================
static var pass_map: Dictionary = {
	# Layer 0 — 基础地形
	0: PassKind.GROUND, 1: PassKind.GROUND, 2: PassKind.GROUND,     # T01 平原
	3: PassKind.DEEP_SEA_OR_CHASM, 4: PassKind.DEEP_SEA_OR_CHASM, 5: PassKind.DEEP_SEA_OR_CHASM,  # T02 海域
	6: PassKind.GROUND, 7: PassKind.GROUND, 8: PassKind.GROUND,     # T03 荒漠
	9: PassKind.WALL, 10: PassKind.WALL, 11: PassKind.WALL,          # T04 山岳·火
	12: PassKind.WALL, 13: PassKind.WALL, 14: PassKind.WALL,         # T05 林莽
	15: PassKind.WALL, 16: PassKind.WALL, 17: PassKind.WALL,         # T06 山峦·密林
	18: PassKind.SHALLOW_WATER, 19: PassKind.DEEP_SEA_OR_CHASM, 20: PassKind.SHALLOW_WATER,  # T07 水泽 (B=深水)
	# Layer 0 — 特殊
	21: PassKind.WALL,      # T08 火山
	22: PassKind.GROUND,    # T09 雪原
	23: PassKind.WALL,      # T10 鬼域
	24: PassKind.GROUND,    # T11 灵脉
	# Layer 3 — 地标 (全部 WALL，边缘不可穿越)
	40: PassKind.WALL, 41: PassKind.WALL, 42: PassKind.GATE_PASS,  # L01-L03
	43: PassKind.WALL, 44: PassKind.WALL, 45: PassKind.WALL,
	46: PassKind.WALL, 47: PassKind.WALL, 48: PassKind.WALL,
	49: PassKind.WALL, 50: PassKind.WALL, 51: PassKind.WALL,
	52: PassKind.WALL, 53: PassKind.WALL, 54: PassKind.WALL,
	55: PassKind.WALL, 56: PassKind.WALL, 57: PassKind.WALL,
	58: PassKind.WALL, 59: PassKind.WALL, 60: PassKind.WALL,
	# 边境
	70: PassKind.GROUND,    # B01 道路
	71: PassKind.GATE_PASS, # B02 关隘
	72: PassKind.GATE_PASS, # B03 境界门控
}


# ================================================================
#  TerrainEffect 定义 (数据行)
#  格式: {speed_mult, damage_per_tick, tick_interval, duration, visual}
# ================================================================
static var effect_table: Dictionary = {
	EffectKind.NONE:              {"speed": 1.0, "dmg": 0, "tick": 0, "duration": 0, "visual": ""},
	EffectKind.SLOW:              {"speed": 0.7, "dmg": 0, "tick": 0, "duration": 0, "visual": "mud_splash"},
	EffectKind.MUD:               {"speed": 0.4, "dmg": 0, "tick": 3.0, "duration": 0, "visual": "sinking"},
	EffectKind.BURN:              {"speed": 0.8, "dmg": 5, "tick": 1.0, "duration": 10.0, "visual": "fire_particles"},
	EffectKind.MIASMA:            {"speed": 0.9, "dmg": 3, "tick": 5.0, "duration": 30.0, "visual": "purple_haze"},
	EffectKind.FROST:             {"speed": 0.6, "dmg": 0, "tick": 0, "duration": 0, "visual": "frost_crystals"},
	EffectKind.SPIRIT_BLESSING:   {"speed": 1.1, "dmg": 0, "tick": 0, "duration": 0, "visual": "golden_motes"},
	EffectKind.SUPPRESS_OR_HEAVY: {"speed": 0.5, "dmg": 0, "tick": 0, "duration": 0, "visual": "gravity_distortion"},
}


# ================================================================
#  TileType → EffectKind 映射 (仅对有特殊效果的 tile)
# ================================================================
static var effect_tile_map: Dictionary = {
	14: EffectKind.MIASMA,            # T05-C 密林·瘴紫
	11: EffectKind.BURN,              # T04-C 岩壁·熔隙
	21: EffectKind.BURN,              # T08 火山
	22: EffectKind.FROST,             # T09 雪原
	23: EffectKind.SUPPRESS_OR_HEAVY, # T10 鬼域
	24: EffectKind.SPIRIT_BLESSING,   # T11 灵脉
	18: EffectKind.SLOW,              # T07-A 水面·青绿 (浅水减速)
	20: EffectKind.MUD,               # T07-C 水面·芦苇 (泥潭)
	8:  EffectKind.SLOW,              # T03-C 沙地·龟裂 (轻微减速)
}


# ================================================================
#  公共查询接口
# ================================================================
static func get_pass_kind(tile_id: int) -> int:
	return pass_map.get(tile_id, PassKind.GROUND)


static func get_effect(tile_id: int) -> int:
	return effect_tile_map.get(tile_id, EffectKind.NONE)


static func get_effect_data(effect: int) -> Dictionary:
	return effect_table.get(effect, effect_table[EffectKind.NONE])


static func is_passable_on_ground(tile_id: int) -> bool:
	var pk := get_pass_kind(tile_id)
	return pk == PassKind.GROUND or pk == PassKind.SHALLOW_WATER


static func is_passable_with_flight(tile_id: int) -> bool:
	var pk := get_pass_kind(tile_id)
	return pk != PassKind.DEEP_SEA_OR_CHASM and pk != PassKind.GATE_PASS
