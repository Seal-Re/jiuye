# ItemDef.gd — 物品数据模型 (item-001)
# L0 纯数据: Id/Name/Kind/Rarity/Effects
extends Resource
class_name ItemDef

# —— 物品类型 ——
enum ItemKind {
	WEAPON = 0, ARMOR = 1, PILL = 2, TALISMAN = 3,
	MATERIAL = 4, QUEST = 5, KEY = 6, SKILL_BOOK = 7
}

# —— 稀有度 (影响颜色+效果强度) ——
enum Rarity { COMMON = 0, UNCOMMON = 1, RARE = 2, EPIC = 3, LEGENDARY = 4 }

# —— 属性 ——
@export var item_id: String = ""
@export var item_name: String = ""
@export var kind: ItemKind = ItemKind.MATERIAL
@export var rarity: Rarity = Rarity.COMMON
@export var tier: int = 1
@export var description: String = ""
@export var stack_size: int = 1
@export var effects: Array[Dictionary] = []  # [{stat, delta}, ...]


# —— 稀有度颜色 ——
const RARITY_COLORS := {
	Rarity.COMMON:    Color(0.6, 0.6, 0.6),
	Rarity.UNCOMMON:  Color(0.2, 0.8, 0.2),
	Rarity.RARE:      Color(0.2, 0.4, 1.0),
	Rarity.EPIC:      Color(0.7, 0.2, 1.0),
	Rarity.LEGENDARY: Color(1.0, 0.7, 0.1),
}

func get_rarity_color() -> Color:
	return RARITY_COLORS.get(rarity, Color.GRAY)


# ═══════════════════════════════════════════════
#  物品词库 (确定性模板, 用于生成描述)
# ═══════════════════════════════════════════════
static var PILL_NAMES := [
	"聚气丹", "凝神散", "金创药", "还魂丹", "筑基灵液", "清心丸",
	"火灵丹", "冰心玉露", "洗髓丹", "凝婴丹", "渡劫丹", "九转还魂丹"
]

static var WEAPON_PREFIX := ["铁", "精铁", "玄铁", "寒冰", "烈焰", "太乙", "真武", "诛仙"]
static var WEAPON_TYPES := ["剑", "刀", "枪", "棍", "扇", "琴", "幡", "印"]

static var ARMOR_NAMES := ["布衣", "道袍", "铁甲", "灵甲", "法袍", "天蚕衣", "金缕衣"]
static var MATERIAL_NAMES := ["灵草", "矿石", "兽骨", "妖丹", "灵石碎片", "玄铁碎片", "陨铁"]


static func generate_random_item(seed: int) -> Dictionary:
	var h := (seed * 73856093) & 0x7FFFFFFF
	var kind := h % 4  # 0-3: pill/weapon/material/armor

	match kind:
		0:
			var name := PILL_NAMES[h % PILL_NAMES.size()]
			var r := (h / 100) % 5 as Rarity
			return {
				"id": "pill_%d" % seed, "name": name, "kind": ItemKind.PILL,
				"rarity": r, "tier": min(r + 1, 5),
				"description": "一颗%s。" % name,
				"effects": [{"stat": "hp", "delta": 20 * (r + 1)}]
			}
		1:
			var prefix := WEAPON_PREFIX[(h / 10) % WEAPON_PREFIX.size()]
			var wtype := WEAPON_TYPES[h % WEAPON_TYPES.size()]
			var r := (h / 50) % 5 as Rarity
			return {
				"id": "weapon_%d" % seed, "name": "%s%s" % [prefix, wtype],
				"kind": ItemKind.WEAPON, "rarity": r, "tier": min(r + 1, 5),
				"description": "一柄%s。锋刃隐隐透出%s光泽。" % [wtype, ["凡铁","寒光","灵光","宝光","神光"][r]],
				"effects": [{"stat": "force", "delta": 3 * (r + 1)}]
			}
		2:
			var name := MATERIAL_NAMES[h % MATERIAL_NAMES.size()]
			return {
				"id": "mat_%d" % seed, "name": name, "kind": ItemKind.MATERIAL,
				"rarity": Rarity.COMMON, "tier": 1,
				"description": "常见的炼丹/炼器材料。",
				"effects": []
			}
		_:
			var name := ARMOR_NAMES[h % ARMOR_NAMES.size()]
			var r := (h / 30) % 5 as Rarity
			return {
				"id": "armor_%d" % seed, "name": name, "kind": ItemKind.ARMOR,
				"rarity": r, "tier": min(r + 1, 5),
				"description": "一件%s。穿上后%s。" % [name, ["略有防护","结实可靠","灵气护体","坚不可摧","诸法不侵"][r]],
				"effects": [{"stat": "constitution", "delta": 2 * (r + 1)}]
			}
