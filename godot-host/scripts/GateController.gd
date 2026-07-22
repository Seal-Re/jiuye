# GateController.gd — 关隘门控交互 (mv-010)
# Area2D 检测玩家靠近关隘 → UI 提示 → 条件检查 → 解锁碰撞
#
# 用法: 挂到每个关隘地标的 Area2D 子节点上

extends Area2D
class_name GateController

# —— 导出属性 ——
@export var gate_name: String = "关隘"
@export var gate_type: int = 0  # 0=关隘需通牒, 1=境界门控
@export var required_realm: int = 0     # 境界门控: 所需 UT
@export var required_pass: bool = false # 关隘: 是否需通牒凭证

# —— 引用 ——
var _player_nearby: bool = false
var _ui_visible: bool = false
var _unlocked: bool = false
var _label: Label


func _ready() -> void:
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

	# UI 提示标签 (初始隐藏)
	_label = Label.new()
	_label.visible = false
	_label.add_theme_color_override("font_color", Color(0.9, 0.85, 0.5, 1.0))
	_label.add_theme_font_size_override("font_size", 13)
	add_child(_label)


func _on_body_entered(body: Node2D) -> void:
	if body is Character2D and body.is_player:
		_player_nearby = true
		if not _unlocked:
			_show_prompt(body)


func _on_body_exited(body: Node2D) -> void:
	if body is Character2D and body.is_player:
		_player_nearby = false
		_hide_prompt()


func _show_prompt(player: Character2D) -> void:
	if _ui_visible:
		return
	_ui_visible = true
	_label.visible = true

	var can_pass := _check_condition(player)
	if can_pass:
		_label.text = "[Enter] 通过 %s" % gate_name
	else:
		if gate_type == 0:
			_label.text = "此乃重地，需持【通牒】方可通行"
		else:
			_label.text = "修为不足，需达【UT%d】方可穿越灵力气墙" % required_realm

	_label.position = Vector2(-_label.size.x / 2, -30)


func _hide_prompt() -> void:
	_ui_visible = false
	_label.visible = false


func _check_condition(player: Character2D) -> bool:
	if gate_type == 0:  # 关隘需通牒
		return required_pass  # 由 Core 侧 GatePass 资格判定
	else:  # 境界门控
		return player.realm_level >= required_realm


# ================================================================
#  输入处理: Enter 键尝试通过
# ================================================================
func _input(event: InputEvent) -> void:
	if not _player_nearby or _unlocked or not _ui_visible:
		return
	if event.is_action_pressed("ui_accept"):
		var player := _get_nearby_player()
		if player == null:
			return
		if _check_condition(player):
			_unlock_gate()
		else:
			_label.text = "条件不满足，无法通过"


func _unlock_gate() -> void:
	_unlocked = true
	_label.text = "✓ 通过 %s" % gate_name

	# 临时禁用碰撞 (1 秒后恢复，足够角色穿过)
	collision_layer = 0
	await get_tree().create_timer(3.0).timeout
	collision_layer = 1
	_unlocked = false
	_label.visible = false
	_ui_visible = false


func _get_nearby_player() -> Character2D:
	for body in get_overlapping_bodies():
		if body is Character2D and body.is_player:
			return body
	return null


# ================================================================
#  外部接口: 从 Core 侧授予通牒资格
# ================================================================
func grant_pass() -> void:
	required_pass = true
	if _player_nearby:
		_show_prompt(_get_nearby_player())
