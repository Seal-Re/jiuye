# DialogueOutcome.gd — 对话结果写回 Core (npc-004)
# 检定成功/失败 → CommandIntent → WorldBridge → Core 事件流
extends Node
class_name DialogueOutcome

var _bridge: Node  # WorldBridge


func _ready() -> void:
	_bridge = get_node("../WorldBridge")


# ================================================================
#  处理对话结果: 根据检定类型+成功/失败 → 生成 CommandIntent
# ================================================================
func apply_outcome(action: String, success: bool, npc_id: int, player_id: int) -> Dictionary:
	var result := {"action": action, "success": success, "effects": []}

	match action:
		"persuade":
			if success:
				result["effects"] = [
					{"type": "relation", "delta": 15},
					{"type": "info", "text": "对方透露了一条附近秘境的消息"}
				]
			else:
				result["effects"] = [
					{"type": "relation", "delta": -5}
				]

		"persuade_hard":
			if success:
				result["effects"] = [
					{"type": "relation", "delta": 20},
					{"type": "quest", "text": "对方委托你一项任务"}
				]
			else:
				result["effects"] = [
					{"type": "relation", "delta": -10}
				]

		"intimidate":
			if success:
				result["effects"] = [
					{"type": "relation", "delta": -15},
					{"type": "item", "text": "对方被迫交出物品"},
					{"type": "grudge_risk", "percent": 30}
				]
			else:
				result["effects"] = [
					{"type": "relation", "delta": -25},
					{"type": "grudge", "text": "对方怀恨在心"},
					{"type": "combat_risk", "percent": 40}
				]

		"deceive":
			if success:
				result["effects"] = [
					{"type": "info_fake", "text": "对方信以为真"},
					{"type": "relation", "delta": 0}
				]
			else:
				result["effects"] = [
					{"type": "relation", "delta": -15},
					{"type": "reputation", "delta": -5}
				]

	# TODO: 将 effects 转化为 CommandIntent 写入 WorldBridge 队列
	# WorldBridge.QueueDialogueOutcome(npc_id, player_id, result)
	_print_result(result)

	return result


func _print_result(result: Dictionary) -> void:
	var action := result["action"]
	var ok := "✓" if result["success"] else "✗"
	print("[DialogueOutcome] %s %s → %s" % [action, ok, str(result["effects"])])


# ================================================================
#  检定投骰 (确定性版, 供单元测试)
# ================================================================
static func roll_check(bonus: int, dc: int) -> Dictionary:
	var roll := randi() % 20 + 1
	var total := roll + bonus
	return {
		"roll": roll,
		"bonus": bonus,
		"total": total,
		"dc": dc,
		"success": total >= dc,
		"critical": roll == 20,
		"fumble": roll == 1
	}


static func get_bonus_for_action(action: String, player_context: Dictionary) -> int:
	match action:
		"persuade":
			return int(player_context.get("reputation", 0) / 10) + int(player_context.get("insight", 0) / 5)
		"persuade_hard":
			return int(player_context.get("reputation", 0) / 15) + int(player_context.get("insight", 0) / 8)
		"intimidate":
			return int(player_context.get("power", 0) / 10) + int(player_context.get("realm", 0)) * 2
		"deceive":
			return int(player_context.get("insight", 0) / 5) + int(player_context.get("reputation", 0) / 20)
		_:
			return 0
