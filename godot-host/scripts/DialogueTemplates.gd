# DialogueTemplates.gd — 规则模板对话引擎 (npc-005 替代)
# 纯确定性: 同 NPC 状态+同玩家状态 → 同对话文本
# 模板 = 人格词库 × 态度基调 × 当前情境
extends Node
class_name DialogueTemplates


# ================================================================
#  人格词库 (ArchType → 口头禅/语气修饰)
# ================================================================
const PERSONALITY: Dictionary = {
	"Martial":  {"greet": "抱拳道", "tone": "豪爽", "particle": "哈哈！"},
	"Scholar":  {"greet": "拱手道", "tone": "儒雅", "particle": "嗯…"},
	"Merchant": {"greet": "搓手笑道", "tone": "精明", "particle": "嘿嘿，" },
	"Rogue":    {"greet": "斜眼道", "tone": "油滑", "particle": "啧，"  },
	"Demonic":  {"greet": "冷声道", "tone": "阴沉", "particle": "哼。"  },
	"Exotic":   {"greet": "用生硬的官话道", "tone": "怪异", "particle": "……"},
}


# ================================================================
#  态度词组 (relation → 开场白/拒绝/接受 词库)
# ================================================================
const ATTITUDE_PHRASES: Dictionary = {
	"friendly": {
		"openings": [
			"{name}面露喜色，{greet}：「{particle}道友来得正好！」",
			"{name}远远看到你便挥手，{greet}：「{particle}好久不见！」",
		],
		"accept": [
			"「{particle}道友开口，自当尽力。」", "「好说好说！」"
		],
		"reject": [
			"「唉，此事我也为难……」面露歉意。"
		],
	},
	"neutral": {
		"openings": [
			"{name}打量了你一番，{greet}：「有何贵干？」",
			"{name}向你微微点头，{greet}：「请讲。」",
		],
		"accept": [
			"「既然道友开口……行吧。」", "「可以是可以，不过……」"
		],
		"reject": [
			"「恕难从命。」摇了摇头。"
		],
	},
	"cold": {
		"openings": [
			"{name}皱眉看着你，{greet}：「有话快说。」",
			"{name}冷淡地瞥了你一眼。",
		],
		"accept": [
			"「……行。但这是最后一次。」"
		],
		"reject": [
			"「免谈。」转身欲走。", "「我不想和你有任何瓜葛。」"
		],
	},
	"hostile": {
		"openings": [
			"{name}手按兵刃，{greet}：「你还敢来？」",
			"{name}眼中闪过杀意，{greet}：「想动手？」",
		],
		"accept": [],  # 敌对不可能接受
		"reject": [
			"「滚！别逼我动手！」手已握紧兵器。",
			"「你我之间没什么好说的。」转身拔剑。"
		],
	},
}


# ================================================================
#  情境信息模板 (npc_state → 额外描述)
# ================================================================
const STATE_PHRASES: Dictionary = {
	"traveling": "行色匆匆，似乎正赶往某处。",
	"idle": "",
	"interacting": "正在与旁人交谈。",
}


# ================================================================
#  生成开场白
# ================================================================
static func generate_opening(ctx: Dictionary) -> String:
	var arch := ctx.get("personality", "Martial")
	var attitude := ctx.get("attitude", "neutral")
	var state := ctx.get("npc_state", "idle")

	var p := PERSONALITY.get(arch, PERSONALITY["Martial"])
	var phrases := ATTITUDE_PHRASES.get(attitude, ATTITUDE_PHRASES["neutral"])
	var openings := phrases["openings"]

	var idx := hash(ctx.get("npc_id", 0) + ctx.get("player_id", 0)) % openings.size()
	var text := openings[idx].format({
		"name": ctx.get("name", "无名"),
		"greet": p["greet"],
		"particle": p["particle"],
	})

	if STATE_PHRASES.has(state) and STATE_PHRASES[state] != "":
		text += " " + STATE_PHRASES[state]

	return text


# ================================================================
#  生成检定结果文本
# ================================================================
static func generate_check_result(
	action: String, success: bool, ctx: Dictionary
) -> String:
	var arch := ctx.get("personality", "Martial")
	var attitude := ctx.get("attitude", "neutral")
	var p := PERSONALITY.get(arch, PERSONALITY["Martial"])
	var phrases := ATTITUDE_PHRASES.get(attitude, ATTITUDE_PHRASES["neutral"])

	if success:
		var accepts := phrases.get("accept", ["……行吧。"])
		var idx := hash(ctx["npc_id"] + 100) % accepts.size()
		return accepts[idx].format({"particle": p["particle"]})
	else:
		var rejects := phrases.get("reject", ["恕难从命。"])
		var idx := hash(ctx["npc_id"] + 200) % rejects.size()
		return rejects[idx].format({"particle": p["particle"]})
