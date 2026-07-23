# DialogueTemplates.gd — 多维矩阵话术引擎 v2
# 状态驱动的三段式组合: [前缀:身份/关系] + [核心:动态动机] + [后缀:性格/威胁]
# 排列组合: 约 10×10×10 = 1000+ 种不重样江湖切口
# 纯确定性——同 state → 同文本
extends Node
class_name DialogueTemplates


# ═══════════════════════════════════════════════
#  三段式词库
# ═══════════════════════════════════════════════

# —— 前缀: 身份/关系 ——
const PREFIX_IDENTITY := [
	"打量了你一眼，", "抚须沉吟，", "拱手一礼，", "眯起眼睛，",
	"叹了口气，", "干笑两声，", "正色道，", "压低声音，",
	"环顾四周后，", "摇了摇头，",
]

const PREFIX_RELATION_FRIENDLY := [
	"面露喜色，", "老远便挥手，", "快步迎来，", "亲热地拍肩，",
]
const PREFIX_RELATION_HOSTILE := [
	"冷哼一声，", "手按兵刃，", "眼中闪过寒光，", "咬牙切齿地，",
]

# —— 核心: 动态动机 ——
const CORE_MOTIVE_WEALTH := [
	"凑近低语：「道友，手头可宽裕？我这有桩买卖……」",
	"盯着你的钱袋：「最近手头紧得很，道友可否周转一二？」",
	"搓着手：「听说西陲万器谷有灵石矿脉的消息……道友可有兴趣？」",
]
const CORE_MOTIVE_GRUDGE := [
	"冷冷道：「你与{grudge_target}的账，还没算清。」",
	"眼中杀意一闪：「{grudge_target}的事，你最好给个交代。」",
	"压低声音：「我劝你离{grudge_target}远点——免得殃及池鱼。」",
]
const CORE_MOTIVE_FEAR := [
	"退后半步：「阁下的名头……我听过。有话好说。」",
	"声音发颤：「你……你想干什么？」",
	"强作镇定：「我与你无冤无仇，不必动粗吧。」",
]
const CORE_MOTIVE_FAVOR := [
	"话锋一转：「其实……在下有一事相求。」",
	"犹豫片刻，终于开口：「道友若能帮我这个忙，必有重谢。」",
	"左右看看，小声道：「有桩差事，不知阁下愿不愿接……」",
]
const CORE_MOTIVE_IDLE := [
	"随口问道：「道友这是要往何处去？」",
	"寒暄道：「近来江湖不太平啊。」",
	"闲聊起来：「说起来，上次{recent_event}之后……」",
]

# —— 后缀: 性格/语气/威胁 ——
const SUFFIX_MARTIAL := [
	"哈哈一笑，豪气干云。", "拍了拍腰间兵器。", "眼中战意一闪。",
]
const SUFFIX_SCHOLAR := [
	"摇头晃脑，引经据典。", "捻须微笑。", "若有所思地点头。",
]
const SUFFIX_MERCHANT := [
	"眼睛滴溜溜转。", "咧嘴一笑，露出金牙。", "手指无意识地搓着铜钱。",
]
const SUFFIX_ROGUE := [
	"贼眉鼠眼地四处张望。", "说完自己先笑了。", "吐了口唾沫。",
]
const SUFFIX_DEMONIC := [
	"嘴角勾起诡异的弧度。", "周身散发出若有若无的黑气。", "冷笑不语。",
]
const SUFFIX_EXOTIC := [
	"用生硬的官话嘟囔了一句。", "做了个古怪的手势。", "眼神深邃，看不出情绪。",
]
const SUFFIX_THREATEN := [
	"——话音未落，杀气已弥漫。", "手已握紧剑柄。", "周围空气似乎凝结了。",
]


# ═══════════════════════════════════════════════
#  环境/地点词库
# ═══════════════════════════════════════════════
const LOCATION_PHRASES := {
	"demon_lair":  ["压低声音：「在这鬼地方待久了，连觉都睡不安稳。」", "警惕地环顾四周：「小声点——这里不是说话的地方。」"],
	"academy":     ["一拱手：「学宫之内，以理服人。」", "指了指论道台：「不如切磋一番，以证所学。」"],
	"market":      ["眼神飘向货摊：「这坊市的东西可不便宜。」", "拉着你到茶摊坐下：「坐下谈，别站着。」"],
	"temple":      ["双手合十：「阿弥陀佛。施主此来，可是要论佛？」", "神色庄严：「佛门净地，莫造杀孽。」"],
	"sect_gate":   ["警惕地看了眼山门方向：「本派近来不太平。」", "压低声音：「门内有大事要发生。」"],
	"wilderness":  ["环顾荒野：「此处不宜久留。」", "握紧兵器：「野外妖兽出没，得小心。」"],
	"palace":      ["毕恭毕敬地行礼：「皇城脚下，不敢放肆。」", "偷瞄了一眼宫门：「朝廷近来风声很紧。」"],
}

const TIME_PHRASES := {
	"injured":  ["面色苍白，说话间咳了几声。", "捂着伤口，额头渗出冷汗。"],
	"post_battle": ["衣襟上还沾着血迹，眼中余悸未消。", "望着远方的硝烟，神情恍惚。"],
	"night":    ["借着月色低声道", "压低声音，怕惊动什么似的"],
	"dawn":     ["迎着晨光伸了个懒腰，", "清晨的薄雾中，"],
}


# ═══════════════════════════════════════════════
#  核心生成函数
# ═══════════════════════════════════════════════
static func generate_opening(ctx: Dictionary) -> String:
	var h := _hash(ctx.get("npc_id", 0), ctx.get("player_id", 0), 0)

	# 1. 选前缀
	var prefix: String
	var relation := ctx.get("relation", 0)
	if relation < -30 and h % 3 == 0:
		prefix = _pick(PREFIX_RELATION_HOSTILE, h + 1)
	elif relation > 30 and h % 3 == 0:
		prefix = _pick(PREFIX_RELATION_FRIENDLY, h + 2)
	else:
		prefix = _pick(PREFIX_IDENTITY, h)

	# 2. 选核心动机
	var core := _pick_core_motive(ctx, h + 3)

	# 3. 选后缀
	var suffix := _pick_suffix(ctx, h + 5)

	# 4. 环境/时间修饰
	var env_phrase := _pick_location_phrase(ctx, h + 7)
	var time_phrase := _pick_time_phrase(ctx, h + 9)

	var name := ctx.get("name", "无名")

	# 拼接
	var text := "%s%s%s%s %s" % [name, prefix, core, suffix, env_phrase]
	if time_phrase != "":
		text += " " + time_phrase

	return text.format(_build_fmt(ctx))


# ═══════════════════════════════════════════════
#  检定结果生成 (复用三段式，态度决定话术)
# ═══════════════════════════════════════════════
static func generate_check_result(
	action: String, success: bool, ctx: Dictionary
) -> String:
	var h := _hash(ctx.get("npc_id", 0), ctx.get("player_id", 0), 100 + (1 if success else 0))
	var name := ctx.get("name", "无名")
	var attitude := ctx.get("attitude", "neutral")

	if success:
		var accept_pool := {
			"persuade":       ["沉吟片刻：「既然道友开口，我信你。」", "点了点头：「此言有理，我应了。」", "面色稍霁：「好，看在道友的面子上。」"],
			"persuade_hard":  ["终于松口：「……好吧。不过下不为例。」", "犹豫良久，终于答应：「行，道友可别让我失望。」"],
			"intimidate":     ["脸色煞白：「好……好说！你要什么？」", "后退一步：「有、有话好说……」", "垂下兵刃：「算你狠。」"],
			"deceive":        ["信以为真：「原来如此……多谢告知。」", "露出恍然大悟的表情：「竟然是这么回事！」"],
		}
		return _pick(accept_pool.get(action, ["……行吧。"]), h).format(_build_fmt(ctx))

	else:
		var reject_pool := {
			"persuade":       ["摇头：「恕难从命。」", "叹了口气：「道理我懂，但这桩事……不成。」"],
			"persuade_hard":  ["态度坚决：「不必多言。我意已决。」", "冷冷道：「这不是面子的问题。」"],
			"intimidate":     ["怒极反笑：「你以为我怕你？来人！」——%s已拔出兵器！" % name, "眼中怒火燃烧：「找死！」——战斗一触即发。"],
			"deceive":        ["皱眉：「你这话……漏洞百出。」", "冷笑：「把我当三岁小孩？」"],
		}
		return _pick(reject_pool.get(action, ["……滚。"]), h).format(_build_fmt(ctx))


# ═══════════════════════════════════════════════
#  内部: 动机选择 (根据 NPC 动态状态)
# ═══════════════════════════════════════════════
static func _pick_core_motive(ctx: Dictionary, h: int) -> String:
	var wealth := ctx.get("npc_wealth", 50)
	var grudge := ctx.get("current_grudge", "")
	var relation := ctx.get("relation", 0)
	var player_power := ctx.get("player_power", 100)

	# 优先级: 恩怨 > 畏惧 > 求财 > 委托 > 闲聊
	if grudge != "":
		return _pick(CORE_MOTIVE_GRUDGE, h).format({"grudge_target": grudge})
	if player_power > 500 and relation < 0:
		return _pick(CORE_MOTIVE_FEAR, h)
	if wealth < 25:
		return _pick(CORE_MOTIVE_WEALTH, h)
	if ctx.get("has_quest", false):
		return _pick(CORE_MOTIVE_FAVOR, h)
	return _pick(CORE_MOTIVE_IDLE, h)


static func _pick_suffix(ctx: Dictionary, h: int) -> String:
	var personality := ctx.get("personality", "Martial")
	var relation := ctx.get("relation", 0)
	var attitude := ctx.get("attitude", "neutral")

	var pool := {
		"Martial": SUFFIX_MARTIAL, "Scholar": SUFFIX_SCHOLAR,
		"Merchant": SUFFIX_MERCHANT, "Rogue": SUFFIX_ROGUE,
		"Demonic": SUFFIX_DEMONIC, "Exotic": SUFFIX_EXOTIC,
	}.get(personality, SUFFIX_MARTIAL)

	var suffix := _pick(pool, h)

	# 敌对态度附加威胁后缀
	if attitude == "hostile" and h % 3 == 0:
		suffix += " " + _pick(SUFFIX_THREATEN, h + 1)

	return suffix


static func _pick_location_phrase(ctx: Dictionary, h: int) -> String:
	var location := ctx.get("location_type", "")
	if location == "" or not LOCATION_PHRASES.has(location):
		return ""
	return _pick(LOCATION_PHRASES[location], h)


static func _pick_time_phrase(ctx: Dictionary, h: int) -> String:
	if ctx.get("npc_hp_low", false):
		return _pick(TIME_PHRASES["injured"], h)
	if ctx.get("post_battle", false):
		return _pick(TIME_PHRASES["post_battle"], h)
	return ""


# ═══════════════════════════════════════════════
#  工具
# ═══════════════════════════════════════════════
static func _pick(pool: Array, h: int) -> String:
	return pool[abs(h) % pool.size()]


static func _hash(a: int, b: int, offset: int) -> int:
	return (a * 73856093 + b * 19349663 + offset * 83492791) & 0x7FFFFFFF


static func _build_fmt(ctx: Dictionary) -> Dictionary:
	return {
		"name": ctx.get("name", "无名"),
		"grudge_target": ctx.get("current_grudge", ""),
		"recent_event": ctx.get("recent_event", "那场大战"),
	}
