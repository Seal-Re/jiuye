# QuestSystem.gd — 委托/任务系统骨架 (npc-006)
# NPC 可发布简单委托: 送信/采集/讨伐/护送
# QuestLog 侧表: 委托状态 + 奖励
extends Node
class_name QuestSystem


# ═══════════════════════════════════════════════
#  委托数据模型
# ═══════════════════════════════════════════════
enum QuestKind { DELIVER = 0, GATHER = 1, HUNT = 2, ESCORT = 3 }
enum QuestState { AVAILABLE = 0, ACCEPTED = 1, IN_PROGRESS = 2, COMPLETED = 3, FAILED = 4, EXPIRED = 5 }

class Quest:
	var id: int
	var kind: QuestKind
	var title: String
	var description: String
	var giver_npc_id: int
	var target_node_id: int
	var target_item: String
	var state: QuestState
	var accepted_tick: int
	var deadline_tick: int
	var reward_reputation: int
	var reward_relation: int
	var reward_item: String


# ═══════════════════════════════════════════════
#  委托词库 (确定性模板)
# ═══════════════════════════════════════════════
const QUEST_TEMPLATES := {
	QuestKind.DELIVER: [
		{"title": "密信送往{target}", "desc": "将此密信亲手交给{target}处的联系人。"},
		{"title": "口信传{target}", "desc": "带个口信到{target}，务必亲口转达。"},
	],
	QuestKind.GATHER: [
		{"title": "采集{item}", "desc": "在附近寻找{item}，我需要一些来炼制丹药。"},
		{"title": "寻{target}灵药", "desc": "听说{target}附近有罕见的药草……"},
	],
	QuestKind.HUNT: [
		{"title": "讨伐{target}妖兽", "desc": "近日{target}妖兽肆虐，为民除害！"},
		{"title": "清除{target}匪患", "desc": "{target}一带贼寇猖獗，官府悬赏捉拿。"},
	],
	QuestKind.ESCORT: [
		{"title": "护送商队至{target}", "desc": "商队要前往{target}，路上不太平，需要人手护送。"},
		{"title": "护送{target}访客", "desc": "有贵客要前往{target}，需人随行护卫。"},
	],
}

const REWARDS := [
	{"reputation": 10, "relation": 10, "item": ""},
	{"reputation": 15, "relation": 15, "item": "灵石×3"},
	{"reputation": 25, "relation": 10, "item": "功法残卷"},
	{"reputation": 20, "relation": 20, "item": "丹药×2"},
	{"reputation": 30, "relation": 15, "item": "法宝碎片"},
]


# ═══════════════════════════════════════════════
#  委托池
# ═══════════════════════════════════════════════
var _quests: Array[Quest] = []
var _next_id: int = 1
var _bridge: Node


func _ready() -> void:
	_bridge = get_node("../WorldBridge")


# 生成委托
func generate_quest(giver_npc_id: int, target_node_id: int, seed: int) -> Quest:
	var h := _hash(seed + giver_npc_id + target_node_id, 0)
	var kind := h % 4 as QuestKind
	var templates := QUEST_TEMPLATES[kind]
	var t := templates[h % templates.size()]
	var reward := REWARDS[h % REWARDS.size()]

	var q := Quest.new()
	q.id = _next_id; _next_id += 1
	q.kind = kind
	q.title = t["title"].format({"target": "节点%d" % target_node_id, "item": "灵草"})
	q.description = t["desc"].format({"target": "节点%d" % target_node_id, "item": "灵草"})
	q.giver_npc_id = giver_npc_id
	q.target_node_id = target_node_id
	q.state = QuestState.AVAILABLE
	q.deadline_tick = 500  # 占位
	q.reward_reputation = reward["reputation"]
	q.reward_relation = reward["relation"]
	q.reward_item = reward["item"]

	_quests.append(q)
	return q


# 接受委托
func accept_quest(quest_id: int, current_tick: int) -> bool:
	for q in _quests:
		if q.id == quest_id and q.state == QuestState.AVAILABLE:
			q.state = QuestState.ACCEPTED
			q.accepted_tick = current_tick
			return true
	return false


# 完成委托
func complete_quest(quest_id: int) -> Dictionary:
	for q in _quests:
		if q.id == quest_id and q.state == QuestState.ACCEPTED:
			q.state = QuestState.COMPLETED
			return {
				"reputation": q.reward_reputation,
				"relation": q.reward_relation,
				"item": q.reward_item,
			}
	return {}


# 超时失败
func check_expired(current_tick: int) -> void:
	for q in _quests:
		if q.state == QuestState.ACCEPTED and current_tick > q.accepted_tick + q.deadline_tick:
			q.state = QuestState.EXPIRED


# 查询可用委托
func get_available_quests() -> Array:
	var result: Array = []
	for q in _quests:
		if q.state == QuestState.AVAILABLE:
			result.append(q)
	return result


func _hash(a: int, b: int) -> int:
	return ((a * 73856093) ^ (b * 19349663)) & 0x7FFFFFFF
