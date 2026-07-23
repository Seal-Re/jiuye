# ContextBuilder.gd — NPC 对话上下文收集 (npc-002)
# 从 Core 侧收集: NPC好感度/门派/恩怨/玩家声望/境界/战力
# 输出结构化 JSON → 供 DialoguePanel 对话生成
extends Node
class_name ContextBuilder

var _bridge: Node  # WorldBridge


func _ready() -> void:
	_bridge = get_node("../WorldBridge")


# ================================================================
#  核心: 构建 NPC 对话上下文
#  输入: NPC Character2D + 玩家 Character2D
#  输出: Dictionary {name, faction, relation, npc_realm, player_realm, ...}
# ================================================================
func build_context(npc: Character2D, player: Character2D) -> Dictionary:
	var ctx: Dictionary = {
		"name": npc.char_name,
		"npc_id": npc.char_id,
		"player_name": player.char_name,
		"player_id": player.char_id,
		"faction": _query_faction(npc),
		"relation": _query_relation(npc.char_id, player.char_id),
		"npc_realm": npc.realm_level,
		"player_realm": player.realm_level,
		"player_reputation": _query_reputation(player.char_id),
		"player_power": _query_power(player),
		"npc_personality": _query_personality(npc),
		"current_grudge": _query_grudge(npc.char_id, player.char_id),
	}

	# 情境修饰: NPC 对你的态度基调
	if ctx["relation"] < -30:
		ctx["attitude"] = "hostile"
	elif ctx["relation"] < 0:
		ctx["attitude"] = "cold"
	elif ctx["relation"] < 30:
		ctx["attitude"] = "neutral"
	else:
		ctx["attitude"] = "friendly"

	# NPC 当前状态
	if npc.is_interacting:
		ctx["npc_state"] = "interacting"
	elif npc.is_moving:
		ctx["npc_state"] = "traveling"
	else:
		ctx["npc_state"] = "idle"

	return ctx


# ================================================================
#  Core 查询 (当前占位——后续 WorldBridge 接入)
# ================================================================
func _query_faction(npc: Character2D) -> String:
	# TODO: 从 Core SectLedger 查 NPC 所属门派
	return npc.npc_data.get("faction", "")


func _query_relation(npc_id: int, player_id: int) -> int:
	# TODO: 从 Core Relations 查好感度 [-100, 100]
	return npc_id * 7 % 100 - 50  # 占位: 确定性伪随机


func _query_reputation(_player_id: int) -> int:
	return 50  # 占位


func _query_power(player: Character2D) -> int:
	return int(player.speed * (1.0 + player.realm_level * 0.2))


func _query_personality(npc: Character2D) -> String:
	# TODO: 从 Core Persona.Archetype 推断
	var archetypes := ["谨慎多疑", "豪爽直率", "阴沉寡言", "圆滑世故"]
	return archetypes[npc.char_id % archetypes.size()]


func _query_grudge(npc_id: int, player_id: int) -> String:
	# TODO: 从 Core GrudgeLedger 查恩怨
	return ""  # 占位: 无恩怨
