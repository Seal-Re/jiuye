# DialoguePanel.gd — NPC 对话面板 (npc-003)
# 古风卷轴风格: NPC 名字 + 对话文本 + 4 检定选项 + 投骰动画
extends Control
class_name DialoguePanel

# —— 引用 ——
var _bridge: Node  # WorldBridge
var _target_npc: Character2D
var _content: RichTextLabel
var _options_container: VBoxContainer
var _result_label: RichTextLabel
var _close_btn: Button
var _visible := false

# 检定结果回调
signal dialogue_outcome(chosen_action: String, success: bool, npc_id: int)


func _ready() -> void:
	_bridge = get_node("../WorldBridge")

	# 面板底色
	var bg := ColorRect.new()
	bg.color = Color(0.06, 0.05, 0.04, 0.95)
	bg.set_anchors_and_offsets_preset(PRESET_FULL_RECT)
	add_child(bg)

	# 金色边框
	var panel := Panel.new()
	panel.set_anchors_and_offsets_preset(PRESET_FULL_RECT)
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0, 0, 0, 0)
	style.border_width_left = style.border_width_right = 2
	style.border_width_top = style.border_width_bottom = 2
	style.border_color = Color(0.55, 0.42, 0.20, 0.7)
	style.corner_radius_top_left = style.corner_radius_top_right = 5
	style.corner_radius_bottom_left = style.corner_radius_bottom_right = 5
	panel.add_theme_stylebox_override("panel", style)
	add_child(panel)

	# NPC 名字
	var name_label := RichTextLabel.new()
	name_label.bbcode_enabled = true
	name_label.fit_content = true
	name_label.text = "[center][color=#c8a860]　对话　[/color][/center]"
	name_label.position = Vector2(0, 8)
	name_label.size = Vector2(320, 22)
	name_label.name = "NameLabel"
	add_child(name_label)

	# 对话文本
	_content = RichTextLabel.new()
	_content.bbcode_enabled = true
	_content.fit_content = true
	_content.position = Vector2(16, 36)
	_content.size = Vector2(288, 60)
	add_child(_content)

	# 选项按钮
	_options_container = VBoxContainer.new()
	_options_container.position = Vector2(16, 100)
	_options_container.size = Vector2(288, 0)
	_options_container.add_theme_constant_override("separation", 4)
	add_child(_options_container)

	# 检定结果
	_result_label = RichTextLabel.new()
	_result_label.bbcode_enabled = true
	_result_label.fit_content = true
	_result_label.position = Vector2(16, 100)
	_result_label.size = Vector2(288, 40)
	_result_label.visible = false
	add_child(_result_label)

	# 关闭按钮
	_close_btn = Button.new()
	_close_btn.text = "✕"
	_close_btn.position = Vector2(290, 8)
	_close_btn.size = Vector2(24, 24)
	_close_btn.add_theme_color_override("font_color", Color(0.75, 0.3, 0.28))
	_close_btn.flat = true
	_close_btn.pressed.connect(_on_close)
	add_child(_close_btn)

	size = Vector2(320, 220)
	position = Vector2(160, 280)
	hide()


func open(npc: Character2D, npc_context: Dictionary) -> void:
	_target_npc = npc
	_visible = true

	# 更新 NPC 名字
	var name_label := get_node("NameLabel") as RichTextLabel
	name_label.text = "[center][color=#c8a860]　%s　[/color][/center]" % npc_context.get("name", npc.char_name)

	# NPC 开场白 (上下文驱动)
	var opening := _generate_opening(npc_context)
	_content.text = "[color=#c8c0a8]%s[/color]" % opening

	# 生成选项
	_clear_options()
	_add_option("礼节性交谈", "persuade", "以礼相待，打探消息")
	_add_option("说服/委托", "persuade_hard", "尝试说服对方 (检定: 声望+悟性)")
	_add_option("威吓/施压", "intimidate", "以势压人 (检定: 战力+境界)")
	_add_option("欺瞒/套话", "deceive", "巧言试探 (检定: 悟性+声望)")

	# 暂停世界
	if _bridge.has_method("set_paused"):
		_bridge.call("set_paused", true)

	show()


func _generate_opening(ctx: Dictionary) -> String:
	var name := ctx.get("name", "无名")
	var faction := ctx.get("faction", "")
	var relation := ctx.get("relation", 0)

	if relation < -30:
		return "%s 冷冷地看着你，手按在兵器上。" % name
	elif relation > 30:
		return "%s 面露喜色，拱手行礼。" % name
	elif faction != "":
		return "%s 打量了你一番。你注意到他衣襟上%s的标记。" % [name, faction]
	else:
		return "%s 向你微微点头，等待你开口。" % name


func _add_option(text: String, action: String, hint: String) -> void:
	var btn := Button.new()
	btn.text = "%s  [color=#666](%s)[/color]" % [text, hint]
	btn.flat = true
	btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
	btn.custom_minimum_size = Vector2(0, 26)
	btn.add_theme_color_override("font_color", Color(0.82, 0.78, 0.65))
	btn.add_theme_font_size_override("font_size", 12)
	btn.pressed.connect(_on_option_pressed.bind(action))
	_options_container.add_child(btn)


func _clear_options() -> void:
	for child in _options_container.get_children():
		child.queue_free()


func _on_option_pressed(action: String) -> void:
	_clear_options()
	_options_container.visible = false

	# 执行检定
	var roll := randi() % 20 + 1  # 1d20
	var bonus := _calculate_bonus(action)
	var dc := randi() % 11 + 10    # DC 10-20
	var success := (roll + bonus) >= dc

	# 显示结果
	var result_text: String
	if success:
		result_text = "[color=#80c080]掷骰 %d + %d = %d ≥ DC%d → 成功！[/color]\n%s" % [roll, bonus, roll + bonus, dc, _success_text(action)]
	else:
		result_text = "[color=#e06060]掷骰 %d + %d = %d < DC%d → 失败...[/color]\n%s" % [roll, bonus, roll + bonus, dc, _failure_text(action)]

	_result_label.text = result_text
	_result_label.visible = true

	# 写回 Core
	dialogue_outcome.emit(action, success, _target_npc.char_id)


func _calculate_bonus(action: String) -> int:
	# 简化的检定加成 (npc-002 完整后从 Core 读取)
	match action:
		"persuade": return 3
		"persuade_hard": return 1
		"intimidate": return 4
		"deceive": return 2
		_: return 0


func _success_text(action: String) -> String:
	match action:
		"persuade", "persuade_hard": return "[color=#b0a080]对方点头应允，关系似乎近了一步。[/color]"
		"intimidate": return "[color=#e0a060]对方面色惨白，不敢违抗。[/color]"
		"deceive": return "[color=#909890]对方信以为真，透露了有用的信息。[/color]"
		_: return "[color=#888]...[/color]"


func _failure_text(action: String) -> String:
	match action:
		"persuade", "persuade_hard": return "[color=#e08060]对方摇头拒绝，态度冷淡了几分。[/color]"
		"intimidate": return "[color=#e04040]对方怒目而视，手按兵刃！[/color]"
		"deceive": return "[color=#c08060]对方识破了你的意图，面露讥讽。[/color]"
		_: return "[color=#888]...[/color]"


func _on_close() -> void:
	_visible = false
	hide()
	if _bridge.has_method("set_paused"):
		_bridge.call("set_paused", false)
GODOTEOF