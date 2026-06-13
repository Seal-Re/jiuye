# World Bible · 《九野》内容补遗（Canonical 附录）

> 本文是《九野·末劫将临》World Bible 的**内容层附录**：机制以 Bible 正文为准, 本附录只填具体内容(势力名册/地理地标/历史锚点/奇遇库/命名池/功法补遗)。
> 由 content-codex workflow 6 内容库 draft 重组(synth 返回前端截断, 已从 journal 恢复全 6 库原文) + synth 第七部一致性自检。
> 批判提出 16 项跨库对账(blocker 3), 见 `2026-06-14-内容补遗-对账backlog.md`, spec-revision 阶段逐条消解。

---

# 第一部 · 初始势力 Landscape 库

# 九野·江湖纪 初始势力 Landscape 库（World Bible §4.7 落地）

> **归属声明**：本库 100% 归属「九野」单一世界。所有势力据「江湖纪（EraIndex=2）」开局态、奉/抗「大胤朝·承熹」名义共主、围绕「道枢裂痕 RiftClock」「灵机下行 AmbientQi」「气运争夺 Fortune」三张力源铺底色。**只铺张力、不写结局**——谁称霸/谁飞升/末劫由谁敲响，全交涌现。
>
> **与已锁系统对齐**：8 FactionType{Sect=0/Court=1/Clan=2/Demonic=3/Gang=4/Merchant=5/Rogue=6/Exotic=7} × AlignmentAxis{正+1/中0/邪-1}；PathBias 软绑 12 路 string key（sword/body/law/ghost/talisman/array/alchemy/artifact/beast/demon/buddha/music）；势力间关系钳 `[-100,100]`（FeudThreshold≤-50，AllyThreshold 取 +40 量级）；据地锚到 §5 命名大区{中州/东海/北漠/西陲/南疆/苗疆/江南}。所有数值=整数示例锚点，**实际魔数待 §8 平衡标定**；加势力=追加 `FactionDef` 数据行（L0 真·零改核心）。
>
> **铺底原则**：预置关系/`War`/`Feud`/`RegimeWariness` 纽带与气运档，但**夺地未触发、复仇弧未点燃**——留足 agent 行动窗口。`PathBias` 用 `PreferredPathKeys`（偏向）/`ForbiddenPathKeys`（禁修）双向软绑，不硬锁个体。

---

## 一、势力名册总表（22 个具名势力，覆 8 FactionType）

> 气运档 `Fortune ∈ [0,FortuneCap=1000]`：980+=国运级 / 800-=道统鼎盛 / 600-=厚底蕴 / 400-=中坚 / 200-=式微/新兴。Align：正+1 / 中0 / 邪-1。

| # | 名号 | Type | Align | 据地大区·地标 | 气运档 | PathBias（偏向→禁修） |
|---|---|---|---|---|---|---|
| 01 | **大胤朝·镇玄司** | Court 1 | 中 0 | 中州·神京（皇城） | 980 | law,artifact,array → demon,ghost |
| 02 | **剑墟道盟** | Sect 0 | 正 +1 | 东海·万剑墟（祖庭） | 880 | sword,array → demon |
| 03 | **太虚玄宗** | Sect 0 | 正 +1 | 中州·玉霄峰（祖庭） | 820 | law,talisman,alchemy → demon,ghost |
| 04 | **铁佛寺（金刚院）** | Sect 0 | 正 +1 | 北漠·须弥岭（祖庭） | 760 | buddha,body → demon,ghost |
| 05 | **焚天乐府** | Sect 0 | 中 0 | 江南·听潮山（祖庭） | 600 | music,law → 无 |
| 06 | **百兽御灵谷** | Sect 0 | 中 0 | 苗疆·万兽渊（祖庭） | 560 | beast,body → 无 |
| 07 | **承熹皇室·萧氏** | Clan 2 | 中 0 | 中州·神京（皇城别苑） | 720 | law,artifact → demon |
| 08 | **琅嬛叶氏** | Clan 2 | 正 +1 | 江南·烟雨坞（坞堡） | 640 | sword,law → demon |
| 09 | **玄铁慕容世家** | Clan 2 | 中 0 | 西陲·镔铁坞（坞堡） | 520 | artifact,body → ghost |
| 10 | **血河魔宫** | Demonic 3 | 邪 -1 | 西陲·血煞原（魔窟） | 700 | demon,body,ghost → buddha |
| 11 | **百蛊渊·万毒门** | Demonic 3 | 邪 -1 | 南疆·百蛊渊（魔窟） | 660 | ghost,alchemy(毒) → buddha |
| 12 | **幽冥鬼道** | Demonic 3 | 邪 -1 | 苗疆·黄泉墟（魔窟/古迹） | 480 | ghost,talisman → buddha |
| 13 | **漕帮（九江盟）** | Gang 4 | 中 0 | 江南·三江口（巨港） | 440 | body,artifact → 无 |
| 14 | **铁掌帮** | Gang 4 | 中 0 | 中州·黑风渡（雄关/渡口） | 360 | body → 无 |
| 15 | **绿林·伏牛寨** | Gang 4 | 中 0 | 中州·伏牛雄关（雄关） | 280 | body,beast → 无 |
| 16 | **万器商会** | Merchant 5 | 中 0 | 西陲·万器谷（巨港/市集） | 920 | artifact,alchemy → demon |
| 17 | **百草堂药行** | Merchant 5 | 中 0 | 江南·杏林镇（市集） | 540 | alchemy → demon,ghost |
| 18 | **通宝镖局钱庄** | Merchant 5 | 中 0 | 中州·四方城（市集） | 500 | artifact,body → 无 |
| 19 | **（散修登记处）** | Rogue 6 | 中 0 | 全图（伪势力聚合） | —（不持势力气运） | 无组织偏向（个体自驱） |
| 20 | **苗疆古蛊一族** | Exotic 7 | 邪 -1 | 苗疆·十万大山（王庭） | 580 | ghost(蛊),beast → buddha,law |
| 21 | **北漠狼鹿王庭** | Exotic 7 | 中 0 | 北漠·瀚海王庭（王庭） | 540 | beast,body → 无 |
| 22 | **东海蛟人鲛宫** | Exotic 7 | 中 0 | 东海·沧溟海眼（王庭/秘境入口） | 460 | law(水),beast → 无 |

> **FactionType 覆盖核查**：Sect×4(02-06) / Court×1(01) / Clan×3(07-09) / Demonic×3(10-12) / Gang×3(13-15) / Merchant×3(16-18) / Rogue×1(19) / Exotic×3(20-22) = 全 8 类型位齐 + 22 具名势力（满足 15-25）。**程序生成世家/帮派若干**在此固定名册之上由 `CodeFactionSource` 追加（§4.7 末行）。

---

## 二、逐势力详档（定位立场 + 张力钩 + 初始关系网）

> 每条关系格式：`→ 目标势力 #：关系值[-100,100]（TieKind）｜缘由（围绕末劫/道枢/气运）`。关系皆**有向**（钳制语义同 `Relations.Adjust`），缘由只给张力不写结局。

### 01 · 大胤朝·镇玄司（Court / 中 / 国运 980）
**定位**：江湖纪名义共主，据中州神京立国，设「镇玄司」节制江湖、查禁魔道、统御灵脉贡赋。国运/龙气=辖地民心×城镇数，辖地最广但灵机层薄、顶阶高手稀，靠制度与气运而非个人战力压场。**末劫底色**：朝廷视道枢为国本，道枢一裂即倾国之力护枢，却也最忌江湖势力借末劫坐大——「功高震主」的猜忌机器。
- → 10 血河魔宫：**-70（War 预置·未触发夺地）**｜血河魔宫西陲坐大、`ResourceHunger=85`，朝廷视其为心腹大患，预置战争纽带但留行动窗口。
- → 02 剑墟道盟：**-20（Neutral·RegimeWariness 起算）**｜既需剑墟镇江湖、又惧其 `MightCache` 与道统气运震主；道枢一裂剑墟气运上升 → `RegimeWariness` 随阈上扬，埋打压/围剿钩。
- → 16 万器商会：**+35（Trade）**｜灵石法宝贡赋命脉，互利通商；但商会气运 920 逼近国运，朝廷暗忌其垄断。

### 02 · 剑墟道盟（Sect·剑修 / 正 / 道统 880）
**定位**：武林泰斗，据东海万剑墟（独孤剑冢式祖庭），以剑修/阵修立道统，号「天下剑出万剑墟」。**末劫底色**：道枢裂痕使万剑墟灵脉剑气共鸣、气运随一裂上升 → 既是护道枢的中流砥柱，又因实力暴涨招朝廷忌惮、招魔道觊觎。
- → 01 大胤朝：**+10（Neutral）**｜奉朝廷为名义共主、共护道枢，但自恃道统不甘受镇玄司辖制，貌合神离。
- → 11 百蛊渊·万毒门：**-65（Feud·gen0 复仇弧种子）**｜两道百年血仇，预置 1 对成员级 `Grudge`（GrudgeKind=SectFeud）投影为复仇弧种子，**谁先动手由涌现定**。
- → 08 琅嬛叶氏：**+45（Alliance）**｜叶氏剑学一脉源出剑墟，世交联盟、共抗魔道南下。

### 03 · 太虚玄宗（Sect·法修 / 正 / 道统 820）
**定位**：中州玉霄峰道门正宗，法修/符修/丹修三脉并立，研「太虚玄清」一类法门，最重道枢气运推演与谶纬。**末劫底色**：玄宗藏有「道枢三裂，末劫重临」谶语的原始推演，对 RiftClock 最敏感，主张趁灵机未尽广收弟子、抢占灵脉冲境。
- → 01 大胤朝：**+25（Trade）**｜为镇玄司供符箓法阵、推演星象，半官方国师渊源。
- → 02 剑墟道盟：**+30（Alliance）**｜正道双璧、共守中原-东海防线，联盟受「魔道共同威胁」供养。
- → 10 血河魔宫：**-55（Feud）**｜法修克煞、道魔不两立；血河魔宫曾血洗玄宗下院，旧怨未清。

### 04 · 铁佛寺·金刚院（Sect·佛修 / 正 / 底蕴 760）
**定位**：北漠须弥岭佛门祖庭，佛修/体修立寺，金刚不坏+降魔克鬼，世代御北漠异族与镇压黄泉墟鬼道。**末劫底色**：佛门视末劫为「劫数」，主张以功德/愿力护生，与朝廷在「御异族」上利益一致却在「是否干涉江湖」上分歧。
- → 12 幽冥鬼道：**-60（Feud）**｜佛克鬼魔为天职，世代镇压黄泉墟鬼道外溢。
- → 01 大胤朝：**+40（Alliance·威胁驱动·消失即瓦解）**｜临时同盟共御北漠异族南侵，威胁消退则联盟 `Strength` 衰减瓦解。
- → 21 北漠狼鹿王庭：**-30（Neutral）**｜数百年边境摩擦，时战时和。

### 05 · 焚天乐府（Sect·乐修 / 中 / 中坚 600）
**定位**：江南听潮山乐修宗门，以琴箫鼓为施法器，焚香玉册一类烈阳音律之力，范围 AOE/控制/增益见长，行踪飘渺、立场中庸。**末劫底色**：乐府信「乱世之音先于乱世之兵」，以音律占卜末劫之期，谁也不投、坐观气运流转，反成各方拉拢对象。
- → 08 琅嬛叶氏：**+30（Marriage·世交）**｜江南同乡世交，联姻通好。
- → 16 万器商会：**+20（Trade）**｜采购音石法器、出售占卜，纯利益往来。
- → 11 百蛊渊·万毒门：**-25（Neutral）**｜乐府净音克蛊毒邪音，互有提防但无死仇。

### 06 · 百兽御灵谷（Sect·御兽 / 中 / 中坚 560）
**定位**：苗疆万兽渊御兽宗门，契约灵兽+祭炼妖丹，群战协同，与苗疆异族同处一域却法理相异（宗门 vs 古族）。**末劫底色**：御灵谷握有苗疆灵兽资源，灵机下行使妖兽躁动、兽潮频发，谷众疲于镇压兽潮，无暇外争。**埋暗线**：参照「灵兽山战时叛变倒戈魔道母宗」母题——御灵谷与幽冥鬼道在「驭兽/驭鬼」技法上同源，预置一条**可被涌现激活的倒戈风险**（非写死）。
- → 20 苗疆古蛊一族：**-40（Feud）**｜同据苗疆争夺灵兽/蛊虫资源，世仇接壤。
- → 12 幽冥鬼道：**-15（Neutral·暗同源钩）**｜驭兽与炼尸技法旁支同源，表面敌对实则暗通，预置低敌意为倒戈风险留口。
- → 04 铁佛寺：**+15（Neutral）**｜共抗南疆魔道，松散善意。

### 07 · 承熹皇室·萧氏（Clan / 中 / 720）
**定位**：大胤朝执政皇族世家（与镇玄司这一「衙门」分层——朝廷=政权 Court，萧氏=血脉门阀 Clan），血脉传承帝王心术+御用法门，掌国库底蕴与联姻网络。**末劫底色**：皇室血脉渐稀（嫡系人才代数下行→`Fortune` 缓降），既是「老牌势力衰退伏笔」，又最怕末劫动摇国本，暗中扶植剑修宗门聚拢气运（参照「宗门挂靠王朝换气运」母题）。
- → 01 大胤朝·镇玄司：**+80（Vassal·镇玄司附庸于皇室政统）**｜镇玄司为皇室所设衙门，上缴气运、受其驱使。
- → 08 琅嬛叶氏：**+50（Marriage）**｜皇室-江南名门联姻世交，气运联结。
- → 09 玄铁慕容世家：**+20（Trade）**｜采买慕容神兵军械装备禁军。

### 08 · 琅嬛叶氏（Clan / 正 / 640）
**定位**：江南烟雨坞千年剑学世家，血脉传剑+藏书底蕴（琅嬛玉洞式藏经），嫡系剑修人才辈出。**末劫底色**：叶氏据江南鱼米之地、Treasury 厚，是正道「联姻世交网」的枢纽，末劫将临时成各方拉拢联盟的核心节点。
- → 02 剑墟道盟：**+45（Alliance）**｜剑学同源、共抗魔道。
- → 07 承熹皇室·萧氏：**+50（Marriage）**｜皇室联姻、气运联结。
- → 09 玄铁慕容世家：**-35（Feud·夺宝旧怨）**｜两世家百年前争一柄祖传神剑结怨（参照「怀璧其罪」母题），预置世仇。

### 09 · 玄铁慕容世家（Clan / 中 / 520）
**定位**：西陲镔铁坞铸兵世家，血脉传器修+体修，世代为朝廷/江湖锻造神兵，据西陲铁矿地利。**末劫底色**：慕容氏垄断西陲精铁，万器商会与血河魔宫皆觊觎其铸兵之秘，夹在商会与魔道之间求存。
- → 16 万器商会：**-20（Neutral·商业竞争）**｜铸兵与法宝交易市场重叠，明争暗斗。
- → 08 琅嬛叶氏：**-35（Feud）**｜争祖传神剑旧怨（与 08 镜像）。
- → 10 血河魔宫：**-30（Neutral·胁迫）**｜魔宫近邻西陲、屡次索要神兵，慕容氏阳奉阴违。

### 10 · 血河魔宫（Demonic·魔修 / 邪 / 700）
**定位**：西陲血煞原魔道总坛，行血修/魔功/炼尸，以吞噬精血气运壮大（理性掠夺者，非脸谱恶）：`Ambition=90, ResourceHunger=85`。**末劫底色**：灵脉总量有限+魔宫资源饥渴极高→预置南下/东进**夺灵脉的「将临之战」**（`War` 纽带预置但未触发夺地）。**埋世代更替钩**：参照「旧霸主衰败→裂解新邪宗」母题，血河魔宫是当世魔道明面霸主，百蛊渊/幽冥鬼道与其是「同源旁支竞争」关系（非附庸），末劫一旧魔道若衰则新邪宗裂解上位。
- → 01 大胤朝：**-70（War 预置·未触发夺地）**｜与朝廷全面战争态势，留行动窗口（与 01 镜像）。
- → 03 太虚玄宗：**-55（Feud）**｜道魔死仇，曾血洗玄宗下院（与 03 镜像）。
- → 11 百蛊渊·万毒门：**-45（Feud·魔道内斗）**｜两大魔道争「邪道气运正统」，参照「五岳同盟却内斗」张力——同邪阵营却互相吞噬。

### 11 · 百蛊渊·万毒门（Demonic·鬼修+毒 / 邪 / 660）
**定位**：南疆百蛊渊魔宗，鬼修炼尸+毒功蛊术，`Ambition=85, ResourceHunger=80`，据南疆瘴疠之地。**末劫底色**：与血河魔宫并称魔道两极，南下夺灵脉，与剑墟道盟有 gen0 复仇弧种子；觊觎道枢碎片以蛊毒催化邪功飞跃。
- → 02 剑墟道盟：**-65（Feud·gen0 复仇弧种子）**｜百年血仇，预置成员级 Grudge（与 02 镜像）。
- → 10 血河魔宫：**-45（Feud·魔道内斗）**｜争邪道气运正统（与 10 镜像）。
- → 20 苗疆古蛊一族：**+15（Trade·蛊术同源）**｜万毒门蛊术源出苗疆古族，暗中交易蛊虫秘术，但古族鄙其堕入魔道，关系脆弱。

### 12 · 幽冥鬼道（Demonic·鬼修 / 邪 / 480）
**定位**：苗疆黄泉墟鬼修邪宗，据上古战场遗迹（HistoryAnchor 葬地钩），炼尸成傀儡战力、养控阴煞，是末劫将临态下「亡者复苏」的潜在引爆点。**末劫底色**：黄泉墟下镇压着神魔纪/百圣纪的古战场亡魂（参照「白骨遮天古战场」「镇世封印」母题），道枢三裂→封印松动→鬼道势力随阶上升，是 RiftClock Stage 推进的受益方。
- → 04 铁佛寺：**-60（Feud）**｜被佛门世代镇压（与 04 镜像）。
- → 06 百兽御灵谷：**-15（Neutral·暗同源钩）**｜驭鬼/驭兽同源，预置倒戈暗线（与 06 镜像）。
- → 11 百蛊渊·万毒门：**+20（Neutral）**｜同据南疆/苗疆魔道、松散呼应，但各炼各道。

### 13 · 漕帮·九江盟（Gang / 中 / 440）
**定位**：江南三江口最大帮派，控漕运渡口收保护费，**弟子按「香袋数」分级**（参照丐帮袋数→现成 `SectMembership.Rank` 字段），地盘民心=控制 Site 的 Wealth 之和。**末劫底色**：漕帮控江南经济命脉，是商会货运的依存对象，乱世中粮道即命脉，谁握漕运谁握气运咽喉。
- → 17 百草堂药行：**+30（Trade）**｜药材货运依存漕帮渡口，互利。
- → 14 铁掌帮：**-40（Feud·火并）**｜两帮争中州-江南漕运地盘火并，预置帮派世仇。
- → 16 万器商会：**+25（Trade）**｜商会大宗货运倚仗漕帮，利益捆绑。

### 14 · 铁掌帮（Gang / 中 / 360）
**定位**：中州黑风渡帮派，控中州水陆要冲，体修硬桥硬马，与漕帮争漕运。**末劫底色**：地处中州枢纽，朝廷镇玄司既用其维持地方又防其坐大，典型「被招安/被镇压」二择的帮派。
- → 13 漕帮·九江盟：**-40（Feud·火并）**｜争漕运地盘（与 13 镜像）。
- → 01 大胤朝：**-10（Neutral·待招安）**｜朝廷对地方帮派的招安/镇压候选。
- → 18 通宝镖局钱庄：**-20（Neutral·收过路费摩擦）**｜铁掌帮向镖局收过路费屡生摩擦。

### 15 · 绿林·伏牛寨（Gang / 中 / 280）
**定位**：中州伏牛雄关山贼帮派，体修+驭兽（养鹰犬），劫掠商旅、占山为王，是最底层武力帮派与散修招募的灰色地带。**末劫底色**：乱世前兆下流民为寇、绿林壮大，是「乱世活下去」底色的直接载体，可被各方收编为打手。
- → 18 通宝镖局钱庄：**-50（Feud·劫镖）**｜劫掠通宝镖局商队，结死仇。
- → 16 万器商会：**-30（Neutral·劫掠目标）**｜觊觎商会运宝车队（参照「怀璧其罪→追杀者」母题）。
- → 19 散修登记处：**+10（Trade·招募）**｜从散修池招揽亡命之徒入伙。

### 16 · 万器商会（Merchant / 中 / 气运 920）
**定位**：西陲万器谷商会，垄断灵石/法宝/丹药硬通货，钱庄+镖局+拍卖行三位一体，Treasury 绝对第一、Might 偏低。**末劫底色**：气运 920 仅次国运但战力孱弱→**易成众矢之的**（怀璧其罪的势力级放大）；灵机下行使灵石愈稀、商会愈富愈危，魔道/绿林/朝廷皆觊觎。
- → 01 大胤朝：**+35（Trade）**｜国之财赋命脉，互利但被暗忌垄断（与 01 镜像）。
- → 10 血河魔宫：**-40（Neutral·被觊觎）**｜魔宫觊觎商会灵石储备，预置敌意。
- → 13 漕帮·九江盟：**+25（Trade）**｜货运依存（与 13 镜像）。

### 17 · 百草堂药行（Merchant / 中 / 540）
**定位**：江南杏林镇药行商会，垄断丹药材+成丹批发，与丹修宗门/医者网络深绑，逐利通商极低 `Conquest`。**末劫底色**：灵机下行使疗伤/续命丹药暴涨为战略物资，百草堂成各方争抢的「后勤命脉」，谁断其货谁失续航。
- → 03 太虚玄宗：**+30（Trade）**｜玄宗丹脉的药材主供，长期契约。
- → 13 漕帮·九江盟：**+30（Trade）**｜药材货运依存漕帮（与 13 镜像）。
- → 11 百蛊渊·万毒门：**-30（Feud·正毒不容）**｜药行解毒克蛊、抵制万毒门毒材外流，结怨。

### 18 · 通宝镖局钱庄（Merchant / 中 / 500）
**定位**：中州四方城镖局+钱庄，承接全图押镖+汇兑，器修/体修镖师护卫，信誉值=商路气运。**末劫底色**：乱世镖货风险陡升、保护费水涨船高，镖局夹在帮派劫掠与朝廷管制之间，靠信誉与镖师战力求存。
- → 15 绿林·伏牛寨：**-50（Feud·被劫镖）**｜苦伏牛寨劫掠久矣（与 15 镜像）。
- → 14 铁掌帮：**-20（Neutral·过路费）**｜屡被收过路费（与 14 镜像）。
- → 16 万器商会：**+20（Trade）**｜为商会押运法宝灵石，业务往来。

### 19 · 散修登记处（Rogue / 中 / 不持势力气运）
**定位**：**伪势力聚合**（非有掌门的实体组织，复用 v1.0 Sect 单例降级体）。散修个体经此 `FactionId` 挂账，`MasterId=null`、Members 松散名册、Treasury=公共集市池；**不参与夺地/灭门/War/Feud**，只做相遇/拜师/招募源池。个体气运归个人侧表 `CultivationState.Resources["fortune"]`，**不持 `Faction.Fortune`**。**末劫底色**：参照「邓太阿独狼剑仙不挂靠宗门」「乱星海散修乐土」——散修是末劫乱世的最大变量池，各势力从此招揽→涌现入门，亦藏不世出的世外高人。
- **无 `FactionTie` 纽带**（散修豁免）；仅经 `_relations` 对各势力持中性基线 0，被各势力按 `AlignmentRelationTable` 招募。

### 20 · 苗疆古蛊一族（Exotic / 邪 / 580）
**定位**：苗疆十万大山化外古族（法理相异于中原），世代炼蛊+图腾血脉，`Isolation` 高、闭关守境、对中原警惕。**末劫底色**：古族传承上古血脉气运（HistoryAnchor 血脉钩），视中原王朝/正道为外侮，灵机下行使古蛊躁动；偶有冲突但不主动南侵，是「守境异族」典范。
- → 06 百兽御灵谷：**-40（Feud）**｜同据苗疆争灵兽/蛊虫资源（与 06 镜像）。
- → 11 百蛊渊·万毒门：**+15（Trade·蛊术同源但鄙其堕魔）**｜蛊术外流给万毒门、却鄙其堕入中原魔道（与 11 镜像）。
- → 01 大胤朝：**-35（Neutral·警惕外侮）**｜视大胤为外侮，闭关御之，关系冷硬。

### 21 · 北漠狼鹿王庭（Exotic / 中 / 540）
**定位**：北漠瀚海游牧异族王庭，驭兽+体修，逐水草而居、控北漠商道关口，与铁佛寺世代摩擦。**末劫底色**：北漠灵机本就稀薄，灵机下行使草场退化→王庭南迁压力增→边境摩擦升级，是朝廷-铁佛寺联盟的预设外压来源（其威胁供养该联盟，威胁消失则盟散）。
- → 04 铁佛寺：**-30（Neutral·边境摩擦）**｜数百年时战时和（与 04 镜像）。
- → 01 大胤朝：**-25（Neutral·和战不定）**｜与朝廷边贸-劫掠交替，纳贡称臣与南侵反复。
- → 16 万器商会：**+15（Trade·边贸）**｜以北漠良马、皮货换商会铁器灵石。

### 22 · 东海蛟人鲛宫（Exotic / 中 / 460）
**定位**：东海沧溟海眼蛟人异族（据海域秘境入口），水属法门+驭海兽，守海疆、不轻易登陆，掌东海航路与海底灵脉。**末劫底色**：道枢碎片传说有一片沉于东海（RiftClock Stage2 道枢碎片秘境配额钩），鲛宫守碎片入口、与剑墟道盟在东海划界共存又暗中博弈。
- → 02 剑墟道盟：**-15（Neutral·海陆划界）**｜东海划界、互不深入，为道枢碎片暗中博弈。
- → 16 万器商会：**+20（Trade·海贸）**｜以东海珍宝、海兽材换商会物资。
- → 01 大胤朝：**0（Neutral）**｜大胤海疆鞭长莫及，互不统属。

---

## 三、`FactionDef` 数据行示例格式（可程序化落地，L0 追加）

> 严格对齐 §4.3 `FactionDef` record 字段顺序：`(DefId, Name, Type, AlignmentAxis, PreferredPathKeys[], ForbiddenPathKeys[], Codes[], BaseReputation, Ambition[0,100], ResourceHunger[0,100], PreferredTerrainMask, RankBands, TypeProfileRef)`。Fortune 初值/关系/纽带由 `CodeFactionSource` 注入运行态（不入 `FactionDef` 只读体）。下示 3 条代表行（朝廷/魔道/散修各一），余 19 条同构。

```jsonc
// === #01 大胤朝·镇玄司（Court 国运级，RegimeWariness 机器）===
{
  "defId": 1, "name": "大胤朝·镇玄司", "type": 1 /*Court*/, "alignmentAxis": 0 /*中*/,
  "preferredPathKeys": ["law", "artifact", "array"],
  "forbiddenPathKeys": ["demon", "ghost"],          // 朝廷查禁魔道/鬼修
  "codes": [/*门规/法度声明式整数谓词:*/ 1001 /*禁邪功*/, 1002 /*灵脉贡赋*/, 1003 /*功高震主→镇*/],
  "baseReputation": 90, "ambition": 60, "resourceHunger": 50,
  "preferredTerrainMask": 0b000011 /*平原+皇城形胜*/,
  "rankBands": { "君主":5, "司首":4, "玄卫":3, "officer":2, "吏":1, "记名":0 },
  "typeProfileRef": 1 /*→FactionTypeProfile[Court]: W_Order高/W_Territory高/FortuneFrom=国运(辖地民心×城镇)*/
}

// === #10 血河魔宫（Demonic 理性掠夺者，将临之战发起侧）===
{
  "defId": 10, "name": "血河魔宫", "type": 3 /*Demonic*/, "alignmentAxis": -1 /*邪*/,
  "preferredPathKeys": ["demon", "body", "ghost"],
  "forbiddenPathKeys": ["buddha"],                  // 魔道排斥佛门净化
  "codes": [3001 /*吞噬精血*/, 3002 /*夺灵脉*/, 3003 /*气运守恒掠夺*/],
  "baseReputation": 30, "ambition": 90, "resourceHunger": 85,  // §4.7 锚定值
  "preferredTerrainMask": 0b010000 /*荒漠血煞原*/,
  "rankBands": { "宫主":5, "血煞长老":4, "魔将":3, "魔徒":2, "血奴":1, "外门":0 },
  "typeProfileRef": 3 /*→FactionTypeProfile[Demonic]: W_Conquest高/W_ResourceHunger高/FortuneFrom=邪道吞噬(守恒转移)*/
}

// === #19 散修登记处（Rogue 伪势力，MasterId=null，不夺地）===
{
  "defId": 19, "name": "(散修登记处)", "type": 6 /*Rogue*/, "alignmentAxis": 0 /*中*/,
  "preferredPathKeys": [],                          // 无组织偏向，个体自驱
  "forbiddenPathKeys": [],
  "codes": [/*散修豁免: INV-FACTION-1 不要求 MasterId*/ 6001 /*公共集市*/, 6002 /*不参与夺地灭门*/],
  "baseReputation": 0, "ambition": 0, "resourceHunger": 0,
  "preferredTerrainMask": 0b111111 /*全图*/,
  "rankBands": null,                               // 无辈分（松散名册）
  "typeProfileRef": 6 /*→FactionTypeProfile[Rogue]: 全权重低/不持Fortune/仅招募源+集市池*/
}
```

**运行态注入伴随表（`CodeFactionSource` 在 `WorldFactory.CreateInitial` 写，seed 控制）**：
```jsonc
// 初始气运档（→ Faction.Fortune，随 Faction.Clone 深拷）
{ "1": 980, "2": 880, "3": 820, "4": 760, "5": 600, "6": 560, "7": 720, "8": 640, "9": 520,
  "10": 700, "11": 660, "12": 480, "13": 440, "14": 360, "15": 280, "16": 920, "17": 540,
  "18": 500, "19": null /*Rogue不持*/, "20": 580, "21": 540, "22": 460 }

// 初始关系边（→ FactionLedger._relations，有向 [-100,100]）+ 结构纽带（→ _ties，TieKind）
// 仅列张力骨架代表边，余按 AlignmentRelationTable + TypeRelationBias 双表生成期注入基线
[
  { "from":1,  "to":10, "rel":-70, "tie":"War"      /*Kind=4，预置未触发夺地*/ },
  { "from":1,  "to":2,  "rel":-20, "tie":"Neutral"  /*RegimeWariness 起算*/ },
  { "from":1,  "to":16, "rel": 35, "tie":"Trade"    /*Kind=5*/ },
  { "from":2,  "to":11, "rel":-65, "tie":"Feud"     /*Kind=1，gen0 复仇弧种子→GrudgeLedger*/ },
  { "from":2,  "to":8,  "rel": 45, "tie":"Alliance" /*Kind=2，威胁供养*/ },
  { "from":3,  "to":2,  "rel": 30, "tie":"Alliance" },
  { "from":4,  "to":1,  "rel": 40, "tie":"Alliance" /*威胁消失即瓦解*/ },
  { "from":4,  "to":12, "rel":-60, "tie":"Feud"     },
  { "from":7,  "to":1,  "rel": 80, "tie":"Vassal"   /*Kind=3，镇玄司附庸皇室政统*/ },
  { "from":8,  "to":9,  "rel":-35, "tie":"Feud"     /*夺神剑旧怨*/ },
  { "from":10, "to":11, "rel":-45, "tie":"Feud"     /*魔道内斗争邪道正统*/ },
  { "from":13, "to":14, "rel":-40, "tie":"Feud"     /*漕运火并*/ },
  { "from":15, "to":18, "rel":-50, "tie":"Feud"     /*劫镖死仇*/ },
  { "from":20, "to":6,  "rel":-40, "tie":"Feud"     /*苗疆灵兽资源争夺*/ }
  // …镜像边与中性基线由 AlignmentRelationTable[(轴A,轴B)] + TypeRelationBias 注入
]
```

---

## 四、张力网设计说明（围绕末劫三源，不写死剧情）

> 22 势力构成的**张力网=整数维+阈值+纽带**，无一行写死结局，全交涌现（对齐 §4.7「底色张力来源」三条）。

1. **道枢裂痕 RiftClock 受益/护卫双阵营**（围绕张力源 B）：
   - **护枢阵营**（道枢=国本/道统根）：大胤朝(01)+太虚玄宗(03)+剑墟道盟(02)+铁佛寺(04)——道枢一裂即护，但内部因「功高震主」(01↔02 RegimeWariness)、「干涉与否」(04↔01)裂痕暗藏。
   - **趁裂阵营**（道枢裂→己方气运随阶上升）：血河魔宫(10)/百蛊渊(11)觊觎道枢碎片催化邪功；幽冥鬼道(12)随封印松动复苏古战场亡魂；东海鲛宫(22)守沉海碎片入口。**Stage 推进谁受益已埋钩，谁先动手由涌现定**。

2. **灵机下行 AmbientQi 的资源挤压链**（围绕张力源 A）：灵机愈枯→灵脉/灵石/丹药/草场愈稀→**魔道 ResourceHunger(85/80) 高→预置南下夺灵脉 War**；商会(16,920)/药行(17)/慕容铸兵(09)因握稀缺战略物资而**怀璧其罪**（气运高 Might 低=众矢之的）；北漠王庭(21)因草场退化生南迁压力。资源挤压是「将临之战」的经济引信。

3. **气运争夺 Fortune 的极化与负反馈**（围绕张力源 C）：
   - **极化顶点**：万器商会(920)/大胤国运(980)/剑墟道统(880)三家气运高位，逼近 FortuneCap → 招围剿/觊觎/猜忌（**强者不通吃**：MightCache 超阈→RegimeWariness↑→打压；高气运者侵蚀道枢更快→引火烧身的负反馈）。
   - **式微伏笔**：承熹皇室萧氏(07)血脉渐稀 Fortune 缓降、绿林伏牛寨(280)/幽冥鬼道(480)新兴待起——为「大战重洗格局/新秀上位/旧魔道衰则新邪宗裂解」留涌现钩（参照「炼血堂衰败→四魔宗裂解」「灵兽山倒戈」母题，均设为**可被激活的暗线**，非写死事件）。

4. **预置但未引爆的复仇弧种子**：剑墟道盟(02)↔百蛊渊(11)的 gen0 成员级 `Grudge`（GrudgeKind=SectFeud，经 `GrudgeLedger` 投影）是唯一显式播种的复仇弧种子，**谁先动手、如何收束由涌现裁定**（对接 §戏剧 5态恩怨弧 + storylet）。其余 Feud 边（08↔09 夺神剑、13↔14 漕运火并、15↔18 劫镖）皆为「怀璧其罪/地盘火并」类**关系阈值钩**，跌破 FeudThreshold(-50)自然升 Feud，不预写战争。

**红线自检**：① 可程序化——全部为 `Faction.Fortune` 整数档 + `_relations[-100,100]` 整数边 + `FactionTie` 枚举纽带 + Ambition/ResourceHunger/RegimeWariness 整数阈值，随机有上限（夺地受 MaxConcurrentWars/WarCooldown 钳，复仇弧受 storylet 频率/强度/并发上限钳）；无自由文本承载逻辑。② 不锁死——加第 23 势力=追加 1 行 `FactionDef`+伴随注入表（L0 真·零改核心）；加新立场=AlignmentAxis×FactionType 叉乘调数据（L0/L1）。③ 严格归属九野——全部锚 §5 命名大区、奉/抗大胤承熹、围绕道枢/末劫/气运底色，零通用化。

**相关 canon 文件路径**：
- World Bible canonical：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-WorldBible-九野-canonical.md`（§4 全谱 Faction 模型 = 本库直接母章；§4.7 = 本库扩展母表；§5 命名大区 = 据地锚点来源）
- 12 路深度设计：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\research\2026-06-13-v1.2-A-修炼路线-每路深度设计.md`（PathBias 的 12 路 PathId string key 来源）

---

# 第二部 · 地理骨架命名库

# 九野 · 地理骨架命名库（GeoCanon 固定层数据行）

> 归属：World Bible canonical《九野·末劫将临》§5（江湖地理架构）固定层 `GeoCanon`。本库只产**固定骨架**（命名大区 + 地标锚点 + 区域邻接），零 RNG、跨纪元不变、逐种子完全一致；秘境/资源点/坊市/散修聚落等**随机微 Site** 留生成期 `mapRng=root.Split(7)` 程序追加（本库不产）。
> 红线对齐：① 可程序化——全整数地利四维 [0,100]、邻接为带 UnifiedTier 硬阈值门控的整数权图；② 不锁死——加大区/地标/边 = 追加一条数据行（L0，零改 `MapGenerator`/`IGeoQuery`）。
> 系统对齐：地标 `FactionDefId` 绑 §4.7 手写种子势力（地图不知门派存在，门派 Seeder 单向读）；`QiDensity` 是「薄灵区(武侠)↔厚灵区(仙侠)」连续梯度轴（§1.1），不存 layer 枚举；资源 `resourceKey` 软绑 12 路（string）；`SiteHazards` 由 `HistorySeeder` 把本库 `HazardSeed` 区域级灾劫展开为代表 Site（承 〇.6，本库只给区域级种子）。

---

## 0. 枚举词表（与 §5.2 schema 对齐，加值即 L1，加行即 L0）

```
ElementKind   : 金 | 木 | 水 | 火 | 土 | 无          // 大区主元素，喂软情境战斗 ±小%（§5.4，属性对属性非路克路）
TerrainKind   : 平原 | 山岳 | 水泽 | 荒漠 | 林莽 | 海域  // 主地形，喂软情境 + 移动 BaseCost
LandmarkKind  : 皇城 | 祖庭 | 魔窟 | 王庭 | 雄关 | 巨港 | 学宫 | 古迹 | 险地 | 秘境口
PassKind      : Open | 关隘需通牒 | 限时开启 | 境界门控    // RegionEdge 通行性质
HazardKind    : 瘴疠 | 妖兽潮 | 鬼雾 | 风暴 | 劫烬       // 区域级灾劫种子（§5.3 / 〇.6）
```

> `QiDensity` 梯度语义钉死（用于全库标定，不另存字段）：**0–25 = 薄灵区**（武侠层主场，凡人/武夫，金庸式江湖）；**26–55 = 衔接带**（梯度过渡，武夫机缘转修士）；**56–100 = 厚灵区**（仙侠层主场，修士引气结丹）。承熹开局全局 `AmbientQi=420`（〇.1），故厚灵区也未到鼎盛，留成长窗口。

---

## 1. 大区表 `RegionDef[]`（7 个命名大区，固定常量）

> 数据行格式：`RegionId | Name | QiDensity | Wealth | Strategic | Peril | ElementAffinity | TerrainClass | 一句定位（premise 叠加）`
> `SiteComposition`/`ResourceTable` 给「主导资源 key 偏置」语义列（生成期随机微 Site 据此加权抽，本库只定偏置，不定具体 Site）。
> NodeId 按 RegionId 顺序前缀和分配：固定地标 Site 占各区低段，随机微 Site 接高段（§5.2）。

| RegionId | Name（大区专名） | QiDensity | Wealth | Strategic | Peril | Element | Terrain | 定位 / premise 叠加 |
|:---:|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| 0 | **中州·神京畿** | 30 | 88 | 92 | 25 | 土 | 平原 | 大胤朝立国腹心、镇玄司节制江湖之地；薄灵衔接带，凡俗武林与朝廷京畿主场；财货/形胜双高，灵气却薄——「共主衰微、灵机下行」最直观的政治中心 |
| 1 | **东海·剑墟域** | 78 | 55 | 60 | 50 | 水 | 海域 | 武林泰斗剑墟道盟祖庭所在；厚灵海域，名山列岛灵脉盛，**「道枢一裂气运随之上升」最先反应的厚灵区**；横渡需高境界，天然割据 |
| 2 | **北漠·铁佛荒原** | 40 | 30 | 70 | 65 | 金 | 荒漠 | 御化外异族的北境苦寒地；铁佛寺据此抵塞外；衔接带偏薄，凶险高、财货寡，佛门以底蕴守边 |
| 3 | **西陲·万器谷地** | 58 | 95 | 55 | 45 | 火 | 山岳 | 万器商会垄断灵石法宝的硬通货之乡；厚灵山岳富矿，Wealth 全图第一、Might 偏弱——「易成众矢之的」的财富洼地 |
| 4 | **南疆·百蛊瘴林** | 70 | 35 | 50 | 88 | 木 | 林莽 | 百蛊渊魔宗盘踞、`ResourceHunger` 驱动北上夺灵脉的邪道渊薮；厚灵林莽但 Peril 全图最高，瘴疠妖兽密布 |
| 5 | **苗疆·十万大山** | 62 | 25 | 40 | 90 | 木 | 山岳 | 苗疆古蛊一族闭关守境之地；厚灵深山、古族传承气运，对中原警惕；最封闭、最凶险，外人难入 |
| 6 | **江南·烟雨水乡** | 22 | 78 | 48 | 20 | 水 | 水泽 | 世家坞堡、商路漕运、市井坊镇密布的薄灵腹地；金庸式凡俗江湖最浓处；富庶承平、灵气最稀，世家血脉渐稀的衰退伏笔之地 |

### 1.1 大区资源偏置 `ResourceTable`（软绑 12 路的 resourceKey 加权，生成期用）

> 每区列「主导 resourceKey → 偏好路（pathAffinityKey）」，决定随机微 Site 抽哪类资源点更密。`resourceKey` 全 string，加新资源种类/新路 = 加 key 映射（L0，零改地图）。

| RegionId | 主导 resourceKey（偏置高→低） | 软绑路（pathAffinity） | premise 备注 |
|:---:|---|---|---|
| 0 中州 | `tribute_field`(贡赋田) / `dao_court`(论道学宫) / `ore` | 法 / 佛 / 器 | 朝廷掌贡赋与法统正当性，灵脉稀 → 资源以财货/人文为主 |
| 1 东海 | `qi_vein`(灵脉)★ / `sword_isle`(剑冢列岛) / `tide_pearl`(潮汐灵珠) | 剑 / 法 / 御兽 | 厚灵区灵脉最密；剑冢供剑修「斩道悟剑」里程 |
| 2 北漠 | `cold_iron`(寒铁矿) / `relic_battlefield`(古战场遗骸)★ / `bone_lair`(妖兽巢) | 器 / 体 / 鬼 | 古战场遗骸→失落传承/跨代恩怨锚（古战场 premise，见 §3） |
| 3 西陲 | `ore`(灵矿)★ / `forge_fire`(地火熔脉) / `spirit_stone`(灵石脉) | 器 / 丹 / 法 | Wealth 第一来源；地火供器修炼器「火候」、丹修炼丹 |
| 4 南疆 | `gu_lair`(蛊虫渊) / `herb_field`(毒药田)★ / `corpse_marsh`(尸沼) | 鬼 / 魔 / 丹 | 魔道/毒丹/炼尸资源密集；瘴疠区危险高 |
| 5 苗疆 | `totem_relic`(图腾古物) / `beast_lair`(灵兽巢)★ / `gu_lair` | 御兽 / 魔 / 鬼 | 古族图腾传承气运、灵兽契约源；外人采集受异族敌视 |
| 6 江南 | `herb_field`(药圃) / `music_stone`(乐石/丝竹)★ / `market_dock`(漕运埠) | 丹 / 乐 / 商路 | 薄灵区以人文/财货资源为主；乐石供乐修施法器 |

★ = 该区 `qi_vein`/古迹型资源点上限 `ResourceCap[key]` 应给得更高（厚灵区/特征区偏置），薄灵区调低。

---

## 2. 地标表 `LandmarkDef[]`（固定锚点，绑 §4.7 种子势力）

> 数据行格式：`Id | RegionId | Kind | Name（地标专名）| FactionDefId（绑势力，空=无主险地/秘境口）| Geo override（地利微调，可空）| premise 钩`
> Id 即固定 Site 的 NodeId 锚（低段，叙事可恒定引用）。`FactionDefId` 由门派 Seeder 单向读 → 把势力 `HomeRegion`/`ControlledSites` 锚到此 Site（§5.2，地图不知门派）。每区 2–4 锚，共 19 锚。

### 中州·神京畿（Region 0）
| Id | Kind | Name | FactionDefId | Geo override | premise 钩 |
|:--:|:--:|---|---|---|---|
| 0 | 皇城 | **神京·紫宸城** | 大胤朝(镇玄司) | Strategic 100 | 名义共主皇城；镇玄司节制江湖、忌惮剑墟道盟（功高震主链）的政令源 |
| 1 | 学宫 | **稷下论道宫** | （无主，朝廷影响） | — | 百圣纪遗存的人文学宫；师承谱系/隐藏血脉 storylet 热点（衔接带，凡俗与修行交汇） |
| 2 | 雄关 | **镇玄关** | 大胤朝(镇玄司) | Peril 35 | 中州门户雄关；`PassKind=关隘需通牒`，朝廷凭此控江湖出入与商路抽成 |

### 东海·剑墟域（Region 1）
| Id | Kind | Name | FactionDefId | Geo override | premise 钩 |
|:--:|:--:|---|---|---|---|
| 3 | 祖庭 | **剑墟·万剑祖庭** | 剑墟道盟 | QiDensity 90 | 武林泰斗祖庭；厚灵之巅、灵脉气运盛，道枢一裂气运随之上升 |
| 4 | 巨港 | **沧澜巨港** | （商会/道盟共用） | Wealth 70 | 东海唯一大港；横渡海域的咽喉，万器商会海路与道盟交汇 |
| 5 | 古迹 | **独孤剑冢** | （无主古迹） | Peril 60 | 失落传承锚（`LostLineageKey`→剑修剑意类）；剑修「入剑冢悟剑+里程」的奇遇点，怀璧者引追杀 |

### 北漠·铁佛荒原（Region 2）
| Id | Kind | Name | FactionDefId | Geo override | premise 钩 |
|:--:|:--:|---|---|---|---|
| 6 | 祖庭 | **铁佛寺·伏魔禅院** | 铁佛寺 | Peril 50 | 佛门底蕴厚、御异族；与朝廷临时同盟（威胁消失即瓦解） |
| 7 | 雄关 | **玉门孤关** | （无主边关） | Strategic 85 | 北境抵塞外的孤关；`PassKind=境界门控`，化外异族入侵的前沿 |
| 8 | 古迹 | **玄昊古战场** | （无主险地） | Peril 80 | **三百年前玄昊大劫主战场遗迹**（古战场 premise 核）；`HazardSeed=劫烬`，高风险高回报资源点 + 跨代恩怨地理坐标 |

### 西陲·万器谷地（Region 3）
| Id | Kind | Name | FactionDefId | Geo override | premise 钩 |
|:--:|:--:|---|---|---|---|
| 9 | 王庭 | **万器谷·百炼总坛** | 万器商会 | Wealth 100 | 灵石/法宝硬通货垄断总坛；Treasury 绝对第一、Might 偏低 → 易成众矢之的 |
| 10 | 巨港 | **流金商埠** | 万器商会 | — | 西陲商路枢纽；`PassKind=Open`，商路气运（贸易 Site 连通度×Wealth）来源 |
| 11 | 险地 | **地火熔渊** | （无主险地） | Peril 70 | 地火熔脉险地；器修「火候」、丹修「炼丹」顶级资源，`HazardSeed=风暴`（地火喷涌） |

### 南疆·百蛊瘴林（Region 4）
| Id | Kind | Name | FactionDefId | Geo override | premise 钩 |
|:--:|:--:|---|---|---|---|
| 12 | 魔窟 | **百蛊渊·噬魂魔宫** | 百蛊渊魔宗 | Peril 90 | 邪道渊薮；`Ambition=90 ResourceHunger=85`，南下夺灵脉的「将临之战」策源（对朝廷/正宗全面 Feud，与剑墟预置 gen0 复仇弧） |
| 13 | 险地 | **万毒尸沼** | （无主险地） | Peril 92 | 炼尸/毒丹资源沼；`HazardSeed=瘴疠`，鬼修炼尸、魔道血煞采集地 |
| 14 | 秘境口 | **幽冥鬼窟·入口** | （无主秘境口） | QiDensity 80 | 周期开启秘境入口（`SecretArchetype`）；鬼修/魂修顶阶传承钩，`EntryGateUT` 高 |

### 苗疆·十万大山（Region 5）
| Id | Kind | Name | FactionDefId | Geo override | premise 钩 |
|:--:|:--:|---|---|---|---|
| 15 | 王庭 | **十万大山·古蛊王庭** | 苗疆古蛊一族 | Peril 88 | 古族闭关守境王庭（`Isolation` 高）；古族传承气运、对中原警惕 |
| 16 | 古迹 | **图腾神冢** | （无主古迹） | QiDensity 75 | 古族图腾血脉传承锚（`Bloodline`→御兽/魔路偏置）；血脉觉醒 storylet 热点 |

### 江南·烟雨水乡（Region 6）
| Id | Kind | Name | FactionDefId | Geo override | premise 钩 |
|:--:|:--:|---|---|---|---|
| 17 | 巨港 | **姑苏·烟雨坊市** | （程序生成世家/帮派据此） | Wealth 90 | 漕运商路与坊市最密；坊市 Hub 邂逅密度锚，程序生成世家/帮派的初始据地 |
| 18 | 险地 | **太湖鬼潮渚** | （无主险地） | Peril 55 | 水泽险地；`HazardSeed=妖兽潮`（鬼潮），薄灵区内罕见的水妖出没点 |

> **premise 三源在地标层的落点**：① **道枢所在**——`World.Rift` 是全局侧表无地理坐标，但其「碎片秘境」（Stage 2 解锁，§2.2）由 `HistorySeeder` 优先落在厚灵区古迹/秘境口（Id 5/14/8/16 候选）；② **灵机最稀之地**——`QiDensity` 最低的 **江南(22)** 与 **中州(30)** 是薄灵地板，末法窗口最直观；③ **古战场**——`玄昊古战场(Id 8)` 为玄昊大劫主战场，叠 `劫烬` HazardSeed + 跨代恩怨 `Grudge` 溯源。

---

## 3. 邻接表 `RegionEdge[]`（无向骨架，带门控）

> 数据行格式：`RegionA — RegionB | BaseCost（整数旅行权）| PassKind | UnifiedTierGate（跨边最低 UT）| premise / 割据语义`
> **INV-GEO-CONNECTED**：忽略门控做 BFS 必须全连通（无孤岛，fail-fast）。门控只限制谁能走，不切断图连通性。中州(0) 是政治中枢，度数最高（辐辏中原）；东海(1)/苗疆(5) 经门控边制造割据与资源垄断。

| RegionA | RegionB | BaseCost | PassKind | UT Gate | premise / 割据语义 |
|---|---|:--:|:--:|:--:|---|
| 0 中州 | 6 江南 | 2 | Open | 0 | 中原腹心与江南水乡通衢；凡俗商路漕运主干，门户大开 |
| 0 中州 | 2 北漠 | 4 | 关隘需通牒 | 0 | 经镇玄关/玉门北上塞外；朝廷凭关控出入，抽商路成 |
| 0 中州 | 3 西陲 | 3 | Open | 0 | 中州与万器谷商路；灵石法宝输入中原的硬通货干线 |
| 0 中州 | 1 东海 | 4 | 关隘需通牒 | 1 | 中原入东海需经沿海雄关；剑墟道盟与朝廷「既合作又防备」的接触面 |
| 6 江南 | 3 西陲 | 5 | Open | 0 | 江南世家与西陲商会的财货-法宝交易线 |
| 6 江南 | 4 南疆 | 5 | 关隘需通牒 | 1 | 江南南缘入百蛊瘴林；魔道北上「将临之战」的渗透路（守方设关） |
| 1 东海 | 3 西陲 | 6 | 境界门控 | 2 | 跨海商路；横渡需 UT2 以上，万器商会海运垄断之源 |
| 4 南疆 | 5 苗疆 | 4 | 境界门控 | 1 | 瘴林深入十万大山；苗疆古族守境，外人难越（异族割据） |
| 4 南疆 | 3 西陲 | 6 | 关隘需通牒 | 1 | 南疆西出取灵矿/丹火；魔道觊觎西陲财富的侧翼 |
| 2 北漠 | 3 西陲 | 7 | 境界门控 | 2 | 北漠荒原西接万器山岳；苦寒难行、需高境界横越 |

> **割据/政治地理读法**（喂 Faction 夺地/联盟/战争，§4.5）：
> - **中州(0)** 度数 4（连江南/北漠/西陲/东海）= 名义共主辐辏全图，但对东海/北漠是「需通牒」半控边 → 共主衰微的地理写照。
> - **东海(1)** 仅 2 边且全门控（UT≥1/2）→ 剑墟道盟据天险半割据，外力难围攻其祖庭。
> - **苗疆(5)** 仅 1 边（连南疆，UT 门控）→ 最封闭，古蛊一族「闭关守境」的地理保证；魔道(南疆)是其唯一邻接，二者「偶有冲突」有了相邻前提。
> - **南疆(4)** 连江南/苗疆/西陲 3 边 → 魔道南下/西掠/扰苗疆的多向策源，「将临之战」的地理扇面。
> - 全图 10 条边、7 节点，忽略门控连通（中州为枢纽，无孤岛），满足 INV-GEO-CONNECTED。

---

## 4. 可扩展性 / 落地说明（L0 纪律）

- **加大区** = `RegionDef[]` 追加一行 + 至少一条 `RegionEdge` 接入骨架（保连通）+ 可选 `LandmarkDef` 锚（L0，零改 `MapGenerator`/`IGeoQuery`/`RegionOf` 二分逻辑——前缀和重算即可）。
- **加地标** = `LandmarkDef[]` 追加一行；绑势力则填 `FactionDefId`（门派 Seeder 单向读），无主填空（险地/古迹/秘境口）。
- **加门控边/改割据** = `RegionEdge[]` 追加/调 `PassKind`+`UnifiedTierGate`（L0）；改不破连通断言即可。
- **HazardSeed 区域级灾劫**（玄昊古战场 `劫烬` 等）由 `HistorySeeder` 在 gen 期展开为代表 Site 的 `SiteHazards`（承 〇.6），本库只给区域级种子（在地标 premise 钩标注），不写 Site 级条目。
- **全库零 RNG、零浮点、整数地利**：`QiDensity/Wealth/Strategic/Peril ∈[0,100]`，`BaseCost/UnifiedTierGate` 整数；逐种子完全一致，是跨纪元不变锚（纪元清算保留古迹/遗物锚点做跨纪元钩子，§7.6）。
- **未定/留生成期**：具体随机微 Site（坊市/客栈/渡口/资源点/秘境实例/散修聚落）、`ResourceCap[key]` 具体数值、`SecretArchetype` 周期/容量魔数——均属随机层 `GeoConfig`，由实现期标定，不在固定骨架内。

**信源对齐**：大区/地标专名严格取自 World Bible §5.2（候选大区 中州/东海/北漠/西陲/南疆/苗疆/江南）与 §4.7（手写种子势力据地：中州·神京/东海·剑墟/西陲·万器谷/北漠·铁佛寺/南疆·百蛊渊/苗疆·十万大山）；玄昊大劫古战场取自 §2.1 premise；三世纪元/道枢/末劫底色取自 §2/§7。命名构词（州/域/谷/渊/墟/冢/关/港/宫 后缀 + 意象前缀）对齐考据简报命名学规律，全部归属九野、不通用化。

---

# 第三部 · HistoryAnchor 历史锚点库

# 九野 · HistoryAnchor 历史锚点库（生成期 HistorySeeder 抽样注入侧表）

> 严格归属《九野·末劫将临》canonical（§7.4）。每条 = `HistoryAnchor` 数据行，**初始条件非时间线**：过去已沉淀为整数残值，运行期不 tick。
> 全部 L0（追加数据行零改播种器）。整数 + 随机有上限（`MaxAnchorsTotal`/`MaxAnchorsPerKind[]`/`MaxAnchorsPerRegion`/`RelicPerRegionCap`/`FortuneTotalBudget`/`AnchorIntensityCap`/`MaxFeudGeneration=3` 三重门控）。
> Era 仅取 `Primordial`(神魔纪 EraIndex0) / `Ancient`(百圣纪 EraIndex1)——**今世·江湖纪不放锚点**（schema 注：今世交三引擎涌现）。
> 抽样：候选 `Preconditions` 全 AND 满足 → `(BaseWeight desc, Id asc)` 稳定排序 → 前缀和整数轮盘（`genRng.Split(GEN_HISTORY=101)` 命名子流）→ 数量/强度/分布先 clamp 后用。
> 名号/区域/势力/path 全取 canon：大区 {中州/东海/北漠/西陲/南疆/苗疆/江南}；玄昊大劫·镇世道枢·大胤朝·镇玄司·承熹；种子势力 {剑墟道盟/百蛊渊魔宗/铁佛寺/万器商会/大胤朝/苗疆古蛊一族}；path {sword_immortal/ti_xiu_hengshi/fa_xiu/array_formation/qixiu_artificer/dan_xiu/gui_xiu_yang_hun/buddhist_golden_body/lei_xiu/yu_shou/…} 及其 canon 功法类目。

---

## 〇 · 数据行 schema 与本库枚举词表（落地约定）

```csharp
record HistoryAnchor(
    int Id, HistoryEra Era,            // Primordial | Ancient（今世不放）
    AnchorKind Kind,                   // Calamity|LostLineage|Battlefield|Feud|Bloodline|Fortune
    int BaseWeight,                    // 轮盘权重（越大越易入选）
    int Intensity /*[0,100]*/,         // 残值量级（≤AnchorIntensityCap）
    IReadOnlyList<AnchorPredicate> Preconditions,  // 全 AND 整数门控
    IReadOnlyList<AnchorEffect> Effects,           // 声明意图→HistorySeeder 翻译成侧表写入
    string PremiseTemplate);           // 仅 Chronicle flavor，零数值路径
AnchorPredicate(AnchorVar Var, CmpOp Op, int Threshold);   // 纯整数比较
AnchorEffect(AnchorEffectKind Kind, int Amount, int Tag);  // Amount=整数量级；Tag=区域Id/FactionDefId/path或类目码
```

**本库使用的 `AnchorVar`（precondition 整数变量，对已生成地图/Faction 快照求值）**：
`region.QiDensity` / `region.Peril` / `region.Wealth` / `region.Strategic`（按 `Tag` 指定 RegionId）、`region.SecretSiteCount`、`faction.Exists`(=0/1, Tag=FactionDefId)、`faction.Type`、`faction.Align`、`faction.Fortune`、`faction.MightCache`、`map.RegionCount`、`config.PathRegistered`(=0/1, Tag=path码)、`anchor.PickedOfKind`(已落地同类计数)。

**本库使用的 `AnchorEffectKind`（声明式落地算子，HistorySeeder 翻译为既有侧表写入）**：
| EffectKind | 落侧表 | Amount 含义 | Tag 含义 |
|---|---|---|---|
| `SetAmbientQiFloorPoint` | `World.Era.AmbientQi` 初值（劫后恢复段整数锚点） | qi 锚点值 | 0（全局） |
| `WriteRegionHazard` | `World.Map.SiteHazards`（区域→代表 Site 展开，承〇.6） | `HazardKind`+Intensity | RegionId |
| `DampenRegionQi` | 该区代表 Site 灵气贫瘠 buff（局部 `Region.QiDensity` 下调影子） | 下调量 | RegionId |
| `PlaceRelicSite` | `World.Map.RelicSite{Kind,Grade,EntryGate}` 挂 `SiteKind.Secret` | Grade(品阶) | RegionId |
| `BindLostLineage` | `RelicSite.LostLineageKey`（string 软绑 12 路某功法类目） | EntryGate(UT门) | path|类目 码 |
| `PlaceBattlefield` | `RelicSite{Kind=Battlefield,Grade}` + 配套 `WriteRegionHazard` | Grade | RegionId |
| `SeedAncestralGrudge` | `GrudgeLedger.Grudge{Generation>0,InheritedFrom=锚点Id,Cause=Ancestral,OriginTick=负}` | Generation(≤3) | 战场RegionId 或 (fA,fB)码 |
| `WriteFactionFeud` | `FactionLedger._relations[(fA,fB)]` 负值（钳[-100,100]，承〇.4） | 负关系值 | (fA,fB) 对码 |
| `SeedBloodline` | `DramaProfile.Bloodline=锚点Id` + `CultivationState` 初始 `rootQuality` 加成（clamp，不破 INV-CROSS） | rootQuality 加成 | path亲和码/FactionDefId |
| `AllocFortune` | 从 `FortuneTotalBudget` 扣减→ `Faction.Fortune`(Tag=FactionDefId) 或个体 `Resources["fortune"]`(命修轴) | 气运量(扣预算) | FactionDefId / 个体原型码 |

> 落地铁律（承 §7.3）：HistorySeeder 绝不新增 Character/Persona/Sect record 字段；恩怨写关系矩阵+可选个体 Grudge，**不写死善恶布尔**（Faction 仍中性 AlignmentAxis）；气运 `AllocFortune` 分配即扣预算，总量守恒；血脉/恩怨依赖的 Faction/地图缺席 → 优雅降级为仅 Chronicle flavor。

---

## 一 · 神魔纪锚点（Era=Primordial / EraIndex0 · 灭世大劫源）
> 神魔互戕+天倾、UT11+ 顶阶传承断绝。贡献 **灭世系数（AmbientQi 初值地板）+ 古战场 + 失落顶阶传承 + 灵气贫瘠区**。Intensity 普遍偏高（残值量级大）。

| Id | 名号 | Kind | BaseWeight | Intensity | Preconditions（AND 整数门控） | Effects（→侧表落地，含落点） | PremiseTemplate（Chronicle flavor） |
|---|---|---|---|---|---|---|---|
| **101** | **玄昊大劫·道枢崩裂** | Calamity | 100 | 92 | `map.RegionCount ≥ 6`（须有完整地图） | `SetAmbientQiFloorPoint(Amount=420, Tag=0)`（钉死今世全局灵机恢复段起点，对齐 `World.Era.AmbientQi=420`）；`DampenRegionQi(Amount=18, Tag=中州)`（神京所在中枢曾为决战中心，灵机损耗最深）；`WriteRegionHazard(Amount=Intensity, Kind=鬼雾, Tag=中州)` | 「三百年前，诸路宗师争『镇世道枢』而两败俱伤，道枢崩裂、一纪元终。今世灵机仅及鼎盛之半——皆此劫余烬。」（解释为何 AmbientQi 只到恢复中段） |
| **102** | **神魔互戕·天倾血野** | Battlefield | 85 | 88 | `region.Peril ≥ 40`（取最凶险区） + `region.QiDensity ≥ 50` | `PlaceBattlefield(Amount=Grade5, Tag=北漠)`（北漠御异族前线，古神魔陈尸处）；`WriteRegionHazard(Amount=85, Kind=妖兽潮, Tag=北漠)`；`SeedAncestralGrudge(Amount=3, Tag=北漠)`（OriginTick 负、Cause=Ancestral，溯源远古某方→今世拾遗者可继承宿怨） | 「神魔陨落处白骨遮天，骨海立无名巨碑。入此战场者，或得残破神兵，或惊起千年怨煞。」 |
| **103** | **太初剑墟·斩道绝响** | LostLineage | 80 | 78 | `faction.Exists(Tag=剑墟道盟)=1` + `config.PathRegistered(Tag=sword_immortal)=1` | `PlaceRelicSite(Amount=Grade5, Tag=东海)`（东海剑墟深处秘境，绑剑墟道盟祖庭）；`BindLostLineage(Amount=EntryGate≈UT5, Tag=sword_immortal|剑意)`（软绑剑修独有「剑意」类目顶阶，呼应 canon「本命剑意·人剑合一」失传残篇） | 「上古剑神斩道至『手中无剑』，飞升前以剑意封于剑墟。今世剑墟道盟仅承其形，那一缕真意，待有缘者以血悟得。」 |
| **104** | **不灭金身·伐毛遗蜕** | LostLineage | 72 | 75 | `config.PathRegistered(Tag=ti_xiu_hengshi)=1` + `region.Peril ≥ 30` | `PlaceRelicSite(Amount=Grade5, Tag=苗疆)`（苗疆十万大山深处古修遗蜕洞）；`BindLostLineage(Amount=EntryGate≈UT6, Tag=ti_xiu_hengshi|横练功)`（绑体修「横练功」顶阶，对应 canon「金刚不坏体神功/不灭金身决」失传残卷） | 「一尊上古体修古神魔之坐化金身，铜皮铁骨历劫不朽。残蜕中犹存伐毛洗髓之法——以自身道基为代价、仅可承一次的牺牲型传承。」（呼应遗蜕「赠者属性永久折损」母题，落地为 EntryGate 高门 + 一次性 RelicSite） |
| **105** | **天人合道·法则余烬** | LostLineage | 60 | 82 | `region.QiDensity ≥ 70`（取灵气最厚区） + `anchor.PickedOfKind(Calamity) ≥ 1`（须玄昊大劫已落地） | `PlaceRelicSite(Amount=Grade6, Tag=东海)`；`BindLostLineage(Amount=EntryGate≈UT8, Tag=fa_xiu|法则)`（绑法修顶阶法则功法，UT8 高门）；`DampenRegionQi(Amount=10, Tag=东海)`（合道引动法则，反噬当地灵脉） | 「曾有天人于此触『法则权限』，合道飞升时灵气倒卷成墟。残存法则烙印需 UT8 修为方能解读——绝大多数今人此生不可及。」（落地为不可达高门遗产，呼应 §1.2 LawPermission） |
| **106** | **绝灵贫瘠·末法之疮** | Calamity | 55 | 70 | `map.RegionCount ≥ 6` + `anchor.PickedOfKind(Calamity) ≥ 1` | `DampenRegionQi(Amount=30, Tag=西陲)`（西陲万器谷一带，大劫波及成灵气荒漠）；`WriteRegionHazard(Amount=70, Kind=风暴, Tag=西陲)` | 「玄昊大劫的冲击波扫过西陲，灵脉至今枯竭。万器商会据此荒漠通商，反成『以财货补灵机』之地。」（解释为何商会据贫灵区却气运高——人造繁荣对冲天然贫瘠） |

---

## 二 · 百圣纪锚点（Era=Ancient / EraIndex1 · 势力起源与气运初分）
> 人族崛起、宗门林立、正邪大战/改朝换代。贡献 **势力起源恩怨 + 气运初始分配 + 隐藏血脉 + 师承谱系根 + 近世古战场**。Intensity 中等。

| Id | 名号 | Kind | BaseWeight | Intensity | Preconditions（AND 整数门控） | Effects（→侧表落地，含落点） | PremiseTemplate（Chronicle flavor） |
|---|---|---|---|---|---|---|---|
| **201** | **正邪大战·剑墟围渊** | Feud | 95 | 80 | `faction.Exists(Tag=剑墟道盟)=1` + `faction.Exists(Tag=百蛊渊魔宗)=1` | `WriteFactionFeud(Amount=-75, Tag=(剑墟道盟,百蛊渊))`（写双向负关系，跌破 FeudThreshold≤-50→开局即 Feud 态）；`SeedAncestralGrudge(Amount=2, Tag=(剑墟道盟,百蛊渊))`（在双方代表角色间预置 gen2 个体 Grudge，喂戏剧首刀复仇线，clamp ≤MaxFeudGeneration=3） | 「百圣纪末，剑墟道盟联正道围攻南疆百蛊渊，两败而未灭。血仇沉淀至今——剑墟一裂、魔宗北望，世仇随时重燃。」（呼应 §4.7 预置强恩怨，**不预写谁赢**） |
| **202** | **镇玄立国·龙气归胤** | Fortune | 90 | 85 | `faction.Exists(Tag=大胤朝)=1` + `faction.Type(Tag=大胤朝)=Court` | `AllocFortune(Amount=300, Tag=大胤朝)`（从 FortuneTotalBudget 拨大额国运/龙气给朝廷，分配即扣预算，守恒）；附 `WriteFactionFeud(Amount=-20, Tag=(大胤朝,剑墟道盟))`（功高震主忌惮基线，对齐 §4.7 关系-20） | 「百圣纪动乱中，大胤太祖据中州神京立国、设镇玄司节制江湖，聚天下龙气于一姓。国运虽盛，对武林泰斗剑墟道盟始终猜忌。」 |
| **203** | **万器谷会·财气垄断** | Fortune | 78 | 72 | `faction.Exists(Tag=万器商会)=1` + `faction.Type(Tag=万器商会)=Merchant` | `AllocFortune(Amount=260, Tag=万器商会)`（拨高商路气运/信誉，扣预算）；`AllocFortune(Amount=-40, Tag=万器商会)` 隐性配衰减提示（香火蒸发由 FactionLedger DecayPct 接管，防通胀） | 「百圣纪末群雄逐鹿，万器谷诸商行合纵成会，以灵石法宝硬通货执天下牛耳。气运冠绝、Might 孱弱——财帛动人心，遂成众矢之的。」 |
| **204** | **铁佛镇北·禅愿守疆** | Fortune | 70 | 65 | `faction.Exists(Tag=铁佛寺)=1` + `config.PathRegistered(Tag=buddhist_golden_body)=1` | `AllocFortune(Amount=180, Tag=铁佛寺)`（拨佛门气运/功德，扣预算）；`SeedBloodline(Amount=rootQuality+6, Tag=buddhist_golden_body)`（在铁佛寺初始僧众中播一脉佛修金身亲和血脉，clamp） | 「百圣纪人族北拓，铁佛寺历代高僧以金身禅愿镇北漠妖氛，护中原免于异族倾覆。佛门气运厚积，金身一脉代代相承。」 |
| **205** | **古蛊封山·苗疆族运** | Bloodline | 75 | 70 | `faction.Exists(Tag=苗疆古蛊一族)=1` + `faction.Align(Tag=苗疆古蛊一族)=-1` | `SeedBloodline(Amount=rootQuality+8, Tag=苗疆古蛊一族)`（在古蛊一族嫡系初始角色写古族图腾血脉，绑 `DramaProfile.Bloodline`，附 `gui_xiu_yang_hun` 路亲和偏置，clamp 不破 INV-CROSS）；`AllocFortune(Amount=120, Tag=苗疆古蛊一族)`（族运/地利气运，扣预算） | 「百圣纪正邪大战后，苗疆古蛊一族退守十万大山、闭关守境，以血脉族纹传承蛊术族运。化外法理相异，对中原素来警惕。」（呼应 Exotic 族运 + 血脉纯度母题，落地为可量化 rootQuality） |
| **206** | **百圣谷·改朝换代古战场** | Battlefield | 68 | 74 | `region.Strategic ≥ 50`（取形胜要地） + `region.Peril ≥ 25` | `PlaceBattlefield(Amount=Grade4, Tag=江南)`（江南膏腴形胜地，百圣纪改朝换代主战场）；`WriteRegionHazard(Amount=60, Kind=瘴疠, Tag=江南)`；`SeedAncestralGrudge(Amount=1, Tag=江南)`（gen1 浅恩怨，溯源近世亡朝旧部） | 「百圣纪末王朝倾覆于此役，十万甲士埋骨江南水泽。古战场遗甲残戈，引来盗墓觊觎者，也埋下亡朝遗民的复国宿怨。」 |
| **207** | **失传雷宗·五雷正法残卷** | LostLineage | 62 | 68 | `config.PathRegistered(Tag=lei_xiu)=1` + `region.QiDensity ≥ 55` | `PlaceRelicSite(Amount=Grade4, Tag=北漠)`（北漠雷击绝顶古宗遗址秘境）；`BindLostLineage(Amount=EntryGate≈UT4, Tag=lei_xiu|雷法)`（绑雷修「五雷正法/掌心雷」破邪一脉残卷，软绑 12 路 L0） | 「百圣纪有雷宗以纯阳五雷破尽天下阴邪，后宗门覆灭、正法散佚。残卷封于北漠雷渊，专破鬼养魂、尸傀、魔功——阴邪体系之天敌。」（呼应 lei_xiu canon 破邪定位） |
| **208** | **师承断绝·孤本玉简** | LostLineage | 58 | 60 | `region.SecretSiteCount ≥ 1`（须该区已生成秘境 Site） + `anchor.PickedOfKind(LostLineage) ≥ 1` | `PlaceRelicSite(Amount=Grade3, Tag=西陲)`（玉简载体 + 残图，形成探索链：指向下一古迹）；`BindLostLineage(Amount=EntryGate≈UT2, Tag=array_formation|阵图)`（绑阵修阵图类残篇，低门入门级） | 「百圣纪一阵法名宿无传人而陨，毕生阵图刻于玉简、藏残图于秘境。后人得玉简者，循残图可启一连串古迹——传承虽断，线索犹存。」（呼应「玉简+残图→探索链」母题） |
| **209** | **世家血脉·渐稀名门** | Bloodline | 65 | 55 | `map.RegionCount ≥ 6` + `anchor.PickedOfKind(Bloodline) ≤ 2`（防血脉锚点过密） | `SeedBloodline(Amount=rootQuality+5, Tag=ti_xiu_hengshi)`（在程序生成的某世家 Clan 嫡系初始角色写一脉武学血脉，绑代际成长偏置，clamp）；隐含 `Fortune` 缓降伏笔（由 FactionLedger 接管，§4.7 老牌衰退钩子） | 「百圣纪鼎盛的武学世家，血脉传至今世已嫡系凋零、底蕴渐稀。一缕祖传根骨血脉尚存于子弟体内——待『血脉觉醒』，或可重现先祖荣光。」（为新秀上位/大战重洗留涌现钩子） |
| **210** | **驭灵旧盟·御兽契传** | Bloodline | 52 | 58 | `config.PathRegistered(Tag=yu_shou)=1` + `region.QiDensity ≥ 45` | `SeedBloodline(Amount=rootQuality+5, Tag=yu_shou)`（在某异族/散修初始角色写御灵亲和血脉，绑 yu_shou 路偏置，clamp）；`PlaceRelicSite(Amount=Grade2, Tag=南疆)`（南疆古驭兽盟遗址，挂灵兽契约残传） | 「百圣纪南疆驭灵旧盟役使群兽称雄，盟破后契法散入血脉。今世偶有天生通灵者，乃其遗胤——与兽缔契，事半功倍。」（呼应御兽契约羁绊母题） |
| **211** | **气运劫余·散修机缘种** | Fortune | 48 | 50 | `map.RegionCount ≥ 6` + `anchor.PickedOfKind(Fortune) ≥ 2`（确保朝廷/商会大额已分配后再播余额） | `AllocFortune(Amount=80, Tag=个体命修原型)`（从 FortuneTotalBudget 余额按 Intensity 分散播给若干命修轴初始角色 `Resources["fortune"]`，扣预算，守恒）；不绑势力（个体气运归个人侧表，承〇.3） | 「百圣纪大战洗尽门阀垄断，零落气运散入草野。今世散修中，偶有天生『机缘』深厚者——气运在身，逢凶化吉、问枢有份。」（命修「机缘」轴初值播种，独立演化） |

---

## 三 · 江湖纪（Era=江湖纪 / EraIndex2）—— **不放锚点**（canon 约束）

> schema 明文 `HistoryEra Era /*Primordial|Ancient；今世不放锚点*/`。江湖纪是 `Clock=0` 运行态，由 12 路 / Faction / 戏剧三引擎涌现，**历史不预写今世**。故本库今世锚点数 = 0。
> 今世的「历史感」来自：① 上两世锚点沉淀的整数残值（古迹/失传/恩怨/血脉/气运初分，已在一、二节落地）；② 运行期 `World.Rift` 硬倒计时跨 {500,250,0} 触发的世界事件（道枢三裂，§2.2）；③ `World.Era.EraTension` 软压力跨 CalamityGate 的纪元清算（§7.6）。三者皆非锚点，不在本库。

---

## 四 · 容量与守恒自检（对齐 HistoryConfig 三重门控）

- **Kind 覆盖**：Calamity×3(101/102?→102 是 Battlefield，Calamity 实为 101/106)、Battlefield×3(102/206/—)、LostLineage×6(103/104/105/207/208/+)、Feud×1(201)、Bloodline×4(204附/205/209/210)、Fortune×5(202/203/204/211/+)。实际入选数由 `MaxAnchorsPerKind[]` clamp——本表 17 行为**候选池**，非全部落地（候选 > 容量，靠权重轮盘择优，制造 run 间多样性）。
- **气运守恒**：所有 `AllocFortune.Amount` 之和（202:300 + 203:260 + 204:180 + 205:120 + 210:— + 211:80 + …）必须 `≤ FortuneTotalBudget`；`Validate()` 守门，超预算 fail-fast。分配即扣减，绝不凭空增发；后续蒸发/掠夺由 FactionLedger / 命修 Backlash 接管。
- **恩怨世代**：所有 `SeedAncestralGrudge.Amount`(Generation) 与 `WriteFactionFeud` 投影的个体 Grudge 代数 `≤ MaxFeudGeneration=3`（=GrudgeLedger.MaxGeneration）。201=gen2、102=gen3、206=gen1，全合规。
- **强度**：所有 `Intensity ≤ AnchorIntensityCap`；神魔纪偏高(70-92)、百圣纪中等(50-85)，残值量级随纪元远近递减，符合「越古越烈但越稀」。
- **分布**：`MaxAnchorsPerRegion` / `RelicPerRegionCap` 限单区古迹堆叠——东海(剑墟,2 LostLineage)、北漠(战场+雷渊+佛门)需 clamp，避免单区古迹爆仓。
- **降级**：依赖 `faction.Exists` 的锚点（201/202/203/204/205）若 `faction=false`，优雅降级为仅 PremiseTemplate Chronicle flavor（不静默崩，承 §7.3 依赖顺序）。

## 五 · 与已锁系统对齐（红线校验）

- **可程序化**：全整数 Predicate（CmpOp 比较）+ 整数 Effect Amount；轮盘 = 前缀和整数轮盘；零自由文本承载逻辑（PremiseTemplate 仅 Chronicle，不进数值路径）。
- **不锁死(L0)**：加一条历史大事件 = 追加一行 HistoryAnchor，零改 HistorySeeder/引擎；加新 path 的失传功法 = 加 `BindLostLineage` 的 `Tag` string key（L0）。
- **侧表纪律**：全部落 `World.Map`(RelicSite/SiteHazards)/`FactionLedger`(_relations/Fortune)/`DramaState`(GrudgeLedger/DramaProfile)/`CultivationState`(rootQuality/fortune)/`World.Era`(AmbientQi 初值)——**零新增 Character/Persona/Sect record 字段**。
- **RngStreamIds**：仅消费 `genRng.Split(GEN_HISTORY=101)` 命名子流（生成期一次性，跳号派生不消费 root，§7.3）；off(history=false) 不构造侧表、不消费子流，DeterminismTests 逐字节回归。
- **软克制对齐**：血脉 `rootQuality` 加成 clamp，绝不破 UnifiedTier 同台当量(INV-CROSS)；气运不进 PowerEngine 主战力（经命修 path adj 在 ±P0/4 闸内影响，§7.5）；失传功法经 `ApplyResource` chokepoint 喂修炼，不绕定和。
- **三轴境界**：`BindLostLineage.EntryGate` 用 UnifiedTier 硬阈值门控（UT2/4/5/6/8 分级），与「移动可达性 UnifiedTier 门控」同构；高门(UT8 法则残烬)落地为今世几乎不可达的传说遗产，呼应 UT12 YAGNI 哲学。

---

# 第四部 · 奇遇 Storylet 库

# 九野奇遇 Storylet 库 — 江湖纪·承熹年间机缘桥段（v1.2 data-only L0）

> 严格归属「九野」World Bible canonical。所有 storylet 落在 `Jianghu.Drama` 声明式 `IStorylet` schema（戏剧引擎 §2），消费 **RngStream6=drama**（点火主流串行 + `dramaRng.Split(arcId)` 弧内子流）；凡效果触碰 per-path 修炼资源/四维/气运者，经既有 chokepoint（`ApplyResource(cult5)` / `StatBlock.Apply` / `Relations.Adjust` / `Faction.ApplyFortune`）落地，drama 层只产 `DomainEvent` 不直接 mutate。
> 「随机但有上限」四件套全整数：cooldown(`CooldownTicks`+`CooldownScope`) / 加权(`BaseWeight`×整数乘子，兜底 w≥1) / 并发(`MaxConcurrentArcs`+`MaxArcsPerCharacter`) / 强度门(`Preconditions` 全 AND 整数阈值)。加一条 = 追加数据行（L0，零改引擎）。
> 命名/数值为示例锚点，魔数待标定（§附录）。地点名取 §4.7/§5 九野固定骨架；功法名取 12 路深度设计；机制名（道枢碎片/灵机下行/末劫/镇玄司/承熹）取 canon。

---

## 〇 · Schema 契约（本库所有行共用，引自戏剧引擎 §2 + A3-FINAL）

```
IStorylet {
  int Id;                                  // 稳定整数键（cooldown/去重/salience 负权）
  ArcKind Arc; int Stage;                  // 归属弧 + 弧内阶段（Encounter 类多为单步 storylet，Arc=Encounter/Revenge）
  long CooldownTicks; int CooldownScope;   // 0=Global 1=PerActor 2=PerPair 3=PerSect
  int BaseWeight;                          // 整数基础权重；× IntensityMul × ArchMul（不合格置0，兜底≥1）
  bool OncePerArc;                         // 弧内一次
  Predicate[] Preconditions;               // 全 AND 整数门控
  Effect[]    Effects;                     // 声明意图 → director 翻译成 DomainEvent → 经 chokepoint
  string      ChronicleTemplate;           // 仅渲染层，绝不进数值路径（武侠味 flavor）
}
Predicate(RoleRef Subject, DramaVar Var, CmpOp Op, int Threshold)        // 纯整数比较
Effect(EffectKind Kind, RoleRef From, RoleRef To, int Amount, int Tag)   // 声明式
```

**RoleRef 角色槽**（本库统一）：`Self`(机缘主角) / `Mentor`(引路者/高人/前辈) / `Foe`(仇敌/觊觎者/对手) / `Beauty`(受困者/结缘对象) / `Sect`(所属/相关势力) / `RivalSect`(敌对势力)。缺槽时该 Effect no-op 优雅降级（承戏剧引擎"A/B 缺席降级"）。

**DramaVar 谓词变量**（读 `IBattleContext`/`CultivationState`/`World.Map`/`FactionLedger` 派生整数，缺 key=0）：
`unifiedTier`(UT0-12) / `realmIndex` / `pathId`(string→hash 比较或专用 PathPred) / `atRegion`(RegionId) / `atSiteKind`(SiteKind) / `siteQiDensity`[0,100] / `innerDemon`[0,100] / `daoHeart`[0,100] / `comprehension` / `res:<key>`(per-path 资源) / `fortuneSelf`(个体气运) / `factionFortune` / `relTo(Foe)`[-100,100] / `grudgeIntensity` / `riftStage`[0,3] / `ambientQi`[0,1000] / `phase`(SectPhase/突破 Phase) / `archetype`(Martial/Cultivator/原型) / `isNight`(0/1) / `hasActiveArc`.

**EffectKind 算子**（翻译为 DomainEvent + 经 chokepoint）：
`AdjResource(res key, ±Δ)`→`ApplyResource`(cult5 钳) / `AdjStat(StatKind, ±Δ)`→`StatBlock.Apply`(运行期单维 clamp[0,30]) / `AdjRelation(±Δ)`→`Relations.Adjust`([-100,100]) / `FormGrudge(Kind,Intensity)` / `AdjGrudge(±Δ)` / `AdjFortuneSelf(±Δ)`→个体 `Resources["fortune"]` / `TransferFortune(Δ)`→守恒(源减目标增) / `AdjFactionFortune(±Δ)`→`Faction.ApplyFortune` / `AddBreakAid(±Δ)`(突破破障辅助) / `AddBreakProgress(±Δ)`(渡劫/小境界进度，硬钳 §奇遇护栏) / `GrantArt(artId)`(习得具名功法→`ChosenArtIds`) / `RealmDelta(±1)`(极罕见，仅"斩道破境/灌顶"类，门控极严) / `AddLifespanBonus(Δ)`(OncePerLife, ≤LEGACY_LIFESPAN_MAX) / `SetFlag(key,0/1)` / `AdjReputation(Faction,±Δ)` / `SpawnArc(ArcKind)`(链式点火下一弧).

---

## 一 · 坠崖/绝境得秘籍宝物 `adventure.fall_get_treasure`
> canon 锚：险地/秘境(SiteKind.Secret + Peril 高) + `RelicSite.LostLineageKey`(失落传承，软绑 12 路) + 道枢裂痕产「道枢碎片」秘境配额(Stage≥2)。母题"坠崖不必死，需引路者/藏宝点两参数"。

### 1101 · 剑墟断崖·独孤遗剑（剑修向，神兵+剑意+习名功法）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1101 / Encounter / 0 |
| Cooldown | 9000 / PerActor（一生此类奇遇宜稀） |
| BaseWeight / OncePerArc | 40 / true |
| Preconditions(全AND) | `Self.pathId == sword_immortal`；`Self.atRegion == 东海·剑墟`；`Self.atSiteKind == Secret`；`Self.siteQiDensity ≥ 30`；`Self.unifiedTier ≥ 2`；`Self.res:jianCheng < 100` |
| Effects | `AdjResource(swordWill, +30)`(钳≤cap=20+5×realm)；`AdjResource(jianCheng, +25)`(剑成度→开光进度)；`GrantArt(sw_dugu9 独孤九剑·破剑式)` 若未习；`AddBreakAid(+15)`；`AdjGrudge(Foe, +0)`(无仇)；`AdjFortuneSelf(+20)` |
| Chronicle | 「{Self} 失足坠入剑墟绝壁，崖底古剑冢列剑四柄，悟剑理三日，携独孤遗式而出。」 |

### 1102 · 道枢碎片·绝壁秘藏（通用，气运+破障，绑道枢裂痕）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1102 / Encounter / 0 |
| Cooldown | 6000 / PerActor |
| BaseWeight / OncePerArc | 30 / true |
| Preconditions | `World.riftStage ≥ 2`(道枢二裂→碎片现世配额开)；`Self.atSiteKind == Secret`；`Self.siteQiDensity ≥ 40`；`Self.unifiedTier ≥ 3` |
| Effects | `AdjFortuneSelf(+40)`(碎片裹挟气运)；`AddBreakProgress(+25)`(硬钳≤gain×3)；`SetFlag(holdsRiftShard, 1)`(标记持碎片→后续可被觊觎，喂 1106/2104)；`AdjResource(<per-path 主资源>, +20)`(按 pathId 路由：剑=swordWill/法=manaPool/器=itemTier 进度…经 ApplyResource) |
| Chronicle | 「道枢一隅崩落，碎片坠于{Self}所探险地深处；拾之，气运灌顶，然怀璧者，江湖皆知。」 |
| 备注 | `holdsRiftShard=1` 是「怀璧其罪」种子：触发 1106(觊觎者)与 2104(夺宝追杀)的前置 flag。守恒律：碎片气运来自全局未分配池，非凭空增发。 |

### 1103 · 北俱险地·万器遗珍（器修向，本命法宝品阶跃迁）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1103 / Encounter / 0 |
| Cooldown | 9000 / PerActor |
| BaseWeight / OncePerArc | 35 / true |
| Preconditions | `Self.pathId == qixiu_artificer`；`Self.atSiteKind == Secret`；`Self.res:itemTier < floor(realmIndex×1.2)+1`(宝滞后于境界才有意义)；`Self.unifiedTier ≥ 2` |
| Effects | `AdjResource(itemTier 进度, +15)`(祭炼推进，仍受 floor(realm×1.2)+1 硬封顶)；`AdjResource(soulBond, +5)`(器魂契合，钳≤20/25)；`GrantArt(qx_rune 落宝夺器纹)` 若未习；`AddBreakAid(+12)` |
| Chronicle | 「{Self} 堕入古炼器宗废墟，地火犹温，一柄胚器认主，器魂相应。」 |

---

## 二 · 误入秘境洞天 `adventure.hidden_realm`
> canon 锚：秘境状态机 Hidden→Open→Closed(CyclePeriod 周期 + EntryGateUT 门控 + Capacity 并发)；`LootTable` 产 (resourceKey,grade) 经 ApplyResource。母题"追逐/逃命坠入 + 机关解谜触发"。

### 1201 · 苗疆古蛊·百蛊玉洞（鬼修/魔道向，煞值+傀儡，绑苗疆异族）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1201 / Encounter / 0 |
| Cooldown | 7000 / PerActor |
| BaseWeight / OncePerArc | 32 / true |
| Preconditions | `Self.atRegion == 苗疆·十万大山`；`Self.atSiteKind == Secret`；`Self.unifiedTier ≥ EntryGateUT(该秘境)`；`Self.pathId ∈ {gui_xiu_yang_hun, ming_fate_causality, fa_xiu}`；`Self.innerDemon < 90`(濒崩者入此凶地必死，门控挡掉) |
| Effects | `AdjResource(shaCharge, +25)`(鬼修煞值；非鬼修路由 devourMeter/manaPool)；`GrantArt(gx_shenwaihuashen 身外化身)` 若鬼修且 UT≥6；`AddBreakAid(+10)`；`AdjResource(innerDemon, +8)`(蛊洞阴煞侵心，代价)；`AdjFortuneSelf(+15)` |
| Chronicle | 「{Self} 为避追兵误入苗疆蛊洞，洞壁蛊纹流转；拜古蛊像、解血纹机关，得养鬼秘术，然阴煞入体，心魔暗长。」 |

### 1202 · 北漠铁佛·古寺地宫（佛修向，愿力+功德+习佛功）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1202 / Encounter / 0 |
| Cooldown | 8000 / PerActor |
| BaseWeight / OncePerArc | 30 / true |
| Preconditions | `Self.atRegion == 北漠·铁佛寺`；`Self.atSiteKind == Secret`；`Self.pathId == buddhist_golden_body`；`Self.daoHeart ≥ 40` |
| Effects | `AdjResource(vow, +20)`(愿力)；`AdjResource(merit, +200)`(功德，merit 进劫 ×2/100)；`AdjResource(goldenLayers, +1)`(金身层)；`GrantArt(bd_dafanbore 大梵般若)` 若未习；`AdjResource(daoHeart, +10)`；`AddBreakAid(+12)` |
| Chronicle | 「{Self} 入铁佛寺枯井地宫，佛骨舍利现光，参古佛壁画，愿力大涨，功德加身。」 |

### 1203 · 通用·灵脉洞天（机关解谜，秘境深度链 山→窟→洞）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1203 / Encounter / 0 |
| Cooldown | 5000 / PerActor |
| BaseWeight / OncePerArc | 28 / true |
| Preconditions | `Self.atSiteKind == Secret`；`Self.siteQiDensity ≥ 60`(灵脉级)；`Self.unifiedTier ≥ EntryGateUT`；`Self.comprehension ≥ 12`(需悟性解机关) |
| Effects | `AdjResource(<per-path 主资源>, +18)`(按 pathId 路由经 ApplyResource)；`AddBreakProgress(+20)`(硬钳)；`AdjResource(comprehension, +5)`；一次性 `AdjFactionFortune(Self.Sect, +10)` 若有 Sect(灵脉归宗门) |
| Chronicle | 「{Self} 循灵气浓郁处，由山缝入古窟、再入洞天，洞中灵脉如练，闭目参悟，福缘自至。」 |

---

## 三 · 高人指点/传功 `encounter.master_guidance`
> canon 锚：师承走 `DramaProfile`(Master/Bloodline 侧表)；灌顶式"内力跨阶转移 + 门派身份继承 + 废原功代价"全局稀缺(1个)。母题"资质平庸+意外破局→灌顶"。RealmDelta 门控极严，仅此类可用。

### 1301 · 剑墟问剑·名宿点化（剑修向，斩道里程+本命剑开光线）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1301 / Encounter / 0 |
| Cooldown | 6000 / PerActor |
| BaseWeight / OncePerArc | 30 / true |
| Preconditions | `Self.pathId == sword_immortal`；`Mentor.unifiedTier ≥ Self.unifiedTier + 3`(须远高于己)；`Mentor.pathId == sword_immortal`；`Self.atRegion == 东海·剑墟`；`Self.daoHeart ≥ 30` |
| Effects | `AdjResource(zhanDaoMiles, +30)`(斩道里程，对齐剑修途径"问剑名宿+30")；`AdjResource(jianCheng, +10)`；`AdjResource(comprehension, +8)`；`AdjRelation(Self→Mentor, +25)`；`SetDramaProfile(Self.Master = Mentor)` 若 Self 无师 |
| Chronicle | 「{Self} 于剑墟问剑前辈{Mentor}，一语点破剑意壁障，斩道里程顿增，执弟子礼。」 |

### 1302 · 绝顶灌顶·机缘唯一继承（全局稀缺灌顶，废原功代价，realm 跃迁）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1302 / Encounter / 0 |
| Cooldown | 30000 / Global（**全局灌顶限量，对齐"机缘唯一继承人 1 个"**） |
| BaseWeight / OncePerArc | 18 / true |
| Preconditions | `Mentor.unifiedTier ≥ 9`(陆地神仙/大乘级前辈)；`Mentor.phase == Declining 或 寿尽将至`；`Self.comprehension ≥ 20`；`Self.daoHeart ≥ 60`；`Self.unifiedTier ≤ 3`(受传者越平庸越爽——低 UT 才合格)；`Self.hasActiveArc == 0` |
| Effects | `RealmDelta(+1)`(灌顶破境，**唯一允许 realm 跃迁的奇遇，门控最严**)；`AdjResource(<per-path 主资源>, +50)`(跨阶灌注)；`AdjStat(Internal, +3)`(运行期 clamp[0,30])；`AddBreakAid(+30)`；`SetDramaProfile(Self.Master = Mentor)`；代价 `AdjResource(daoHeart, -10)`(根基不稳暗患) |
| Chronicle | 「{Mentor} 油尽灯枯，择{Self}为衣钵，倾毕生修为灌体；{Self} 经脉重塑，一夜越境，然根基浮浅，道心微荡。」 |
| 备注 | 守恒/反制：`Mentor` 灌顶后 `RealmDelta(-N)` 或转 Ascended/陨落(由 Lifecycle)，能量非凭空；先天加成 clamp 不破 INV-CROSS(同 UT 当量)。 |

### 1303 · 论道·同节点修士点拨（被动涌现，破"铺路税"，不需先 Roam）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1303 / Encounter / 0 |
| Cooldown | 2000 / PerPair（同一对师徒冷却） |
| BaseWeight / OncePerArc | 22 / false |
| Preconditions | `Mentor.unifiedTier ≥ Self.unifiedTier + 1`；`Mentor.atSiteKind == Self.atSiteKind`(同节点，**被动触发于既有轨迹**)；`Mentor.atRegion == Self.atRegion`；`Self.phase == Bottleneck` |
| Effects | `AdjResource(comprehension, +4)`；`AddBreakAid(+6)`；`AdjResource(daoHeart, +3)`；`AdjRelation(Self→Mentor, +8)` |
| Chronicle | 「{Self} 卡于瓶颈，恰遇{Mentor}过此，论道半日，茅塞渐开。」 |
| 备注 | 低权重高频，对冲太吾"铺路税"：论道被动涌现，不强制 Roam。同 Category 连命中后 salience 负权衰减(×(100−recentHits×20)/100)。 |

---

## 四 · 仇家追杀/灭门复仇 `conflict.vendetta_hunt`
> canon 锚：复仇弧 5 态(Victimized→BuildUp→Hunting→Showdown→Resolved/Abandoned)；`GrudgeLedger`(Insult/Maiming/Slaughter, Intensity[0,100], Generation)；`FeudThreshold≤-50` 势力世仇下沉个体 Grudge。母题"怀璧其罪→怀璧者自动生成觊觎者/追杀者"(两条全局稀缺律之二)。这些是**复仇弧的 storylet 节点**，归 Arc=Revenge 各 Stage。

### 1401 · 镖局血洗·灭门埋仇（Revenge·Victimized 点火节点，绑镇玄司管辖外江湖）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1401 / Revenge / Victimized |
| Cooldown | 8000 / PerPair |
| BaseWeight / OncePerArc | 36 / true |
| Preconditions | `grudgeIntensity(Self,Foe) ≥ GrudgeIgniteThreshold`；`Foe.unifiedTier ≥ Self.unifiedTier + MassacreGap`(碾压差才血洗)；`IsAlive(Self) && IsAlive(Foe)` |
| Effects | `FormGrudge(Self→Foe, Kind=Slaughter, Intensity=85)`；负 valence Memory 注入(大恨不被 MemoryStore 淘汰)；`AdjRelation(Self→Foe, -60)`(镜像上限内负关系驱动 RuleBrain notFoe)；`AdjFortuneSelf(Self, -15)`(家道中落)；`TransferFortune(Self→Foe, 10)`(夺其底蕴，守恒) |
| Chronicle | 「{Foe} 觊觎{Self}家传之物，血洗其门；{Self} 侥幸逃生，血海深仇，自此入心。」 |
| 备注 | 点火即入弧；后续 `SetGoal(Self, Advance)` 让 RuleBrain 1500 权重自发疯狂修炼(BuildUp)。非致死(v1.2.0)：重创=关系崩坏+恩怨转移。 |

### 1402 · 怀璧追杀·觊觎者上门（Revenge·Hunting 推进，怀璧其罪律落地）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1402 / Revenge / Hunting |
| Cooldown | 3000 / PerPair |
| BaseWeight / OncePerArc | 30 / false |
| Preconditions | `Self.flag:holdsRiftShard == 1 或 Self.res:itemTier ≥ 6 或 Self.fortuneSelf ≥ 500`(持稀有物=怀璧)；`Foe.ambition ≥ 60`(觊觎者野心)；`relTo(Self,Foe) ≤ 0`；`IsAlive(Foe)` |
| Effects | `FormGrudge(Foe→Self, Kind=Maiming, Intensity=55)`(**物品自动生成追杀者**)；`AdjGrudge(Self→Foe, +20)`；`AdjRelation(Self↔Foe, -30)`；`SpawnArc(Revenge)` 若并发未满(Foe 视角的夺宝弧) |
| Chronicle | 「{Self} 怀道枢碎片/至宝之名播于江湖，{Foe} 起觊觎之心，遣人追杀，欲夺宝杀人。」 |
| 备注 | 直接落地 canon 两条全局稀缺律之"怀璧其罪：持有稀有物→自动生成觊觎者关系"。受 MaxConcurrentArcs/对子 cooldown 约束防"全员追杀"。 |

### 1403 · 狭路相逢·复仇了断（Revenge·Showdown 结算，复用 SparAction，非致死）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1403 / Revenge / Showdown |
| Cooldown | 1000 / PerPair |
| BaseWeight / OncePerArc | 50 / true |
| Preconditions | `Self.atSiteKind == Foe.atSiteKind && Self.atRegion == Foe.atRegion`(同节点，由 Goal/Hunting 钩自发促成)；`grudgeIntensity(Self,Foe) ≥ GrudgeIgniteThreshold`；或 `arcAge ≥ ShowdownTimeout`(超时强制结算，防死锁) |
| Effects | 复用 `SparAction` 五段式判胜负(境界压制×per-path+情境+战技+有界 roll)；`RevengeConsummated`：`AdjRelation(Self↔Foe, →-100)`；恩怨化解/转移(`AdjGrudge → 0`)；胜方 `TransferFortune(败→胜, 30)`(守恒)；败方若持碎片则 `SetFlag(holdsRiftShard, 转移给胜方)` |
| Chronicle | 「{Self} 与{Foe} 终在{atRegion}狭路相逢，多年血仇，一战了断。」 |
| 备注 | 非致死(v1.2.0)：重创=关系到 -100 + 恩怨化解/转移。仇人先寿尽则弧 Resolved/Abandoned；复仇者寿尽且仇未了→子嗣/弟子继承 Grudge(Gen+1, ×InheritDecayPct, ≤MaxGeneration=3)→点燃下一弧(跨代链)。 |

---

## 五 · 比武招亲 `event.tournament_marriage`
> canon 锚：戏剧 storylet 105"比武招亲(埋 Insult 种子，情仇钩)"；同一事件同改两条关系(好感+/结仇+)；可暗设锁人筛选条件。归 Arc=Encounter，可埋复仇弧种子。

### 1501 · 世家比武招亲·锁人擂（结缘/结仇双产出，绑世家 Faction）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1501 / Encounter / 0 |
| Cooldown | 6000 / PerSect（同世家招亲冷却） |
| BaseWeight / OncePerArc | 26 / true |
| Preconditions | `RivalSect.Type == Clan`(世家设擂)；`Self.unifiedTier ≥ 1`；`Self.archetype ∈ {Martial, Cultivator}`；`Beauty.Sect == RivalSect`(招亲方千金)；(暗设锁人) `Self.unifiedTier ∈ [筛选带]` |
| Effects | 复用 `SparAction` 判擂台胜负(可作弊：黑哨 = 给守方 +情境 adj 在 ±P0/4 内)；胜则 `AdjRelation(Self↔Beauty, +40)`(结缘) + `AdjRelation(Self↔RivalSect, +20)`；**悔约分支**(随机 dramaRng.Split)：`FormGrudge(Self→RivalSect 代表, Insult, 40)` + `AdjRelation(Self↔RivalSect, -50)`(打赢却毁约→结仇) |
| Chronicle | 「{RivalSect} 设擂招亲，明限门第实为锁人；{Self} 连胜三场，或抱得佳人，或反遭悔婚结怨。」 |
| 备注 | 同一事件同改两条关系(canon"救美双产出"同理)。悔约埋 Insult 种子可升 Feud→喂复仇弧(1401/2104)。 |

### 1502 · 宗门联姻擂·气运联结（联盟向，绑势力联姻 +MarriageBond）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1502 / Encounter / 0 |
| Cooldown | 8000 / PerPair（同两势力冷却，防刷） |
| BaseWeight / OncePerArc | 20 / true |
| Preconditions | `Self.Sect != null && Beauty.Sect != null`；`Self.Sect.AlignmentAxis == Beauty.Sect.AlignmentAxis`(同轴易联姻)；`relTo(两 Sect) ≥ 0`；`Self.unifiedTier ≥ 3` |
| Effects | `AdjRelation(两 Sect, +40 MarriageBond)`(升 `FactionTie.Marriage`)；`TransferFortune` 双向(嫁妆/聘礼，守恒)；`AdjRelation(Self↔Beauty, +30)`；`AdjFactionFortune(两 Sect, +5)`(世交气运) |
| Chronicle | 「两宗以武会亲，{Self} 胜出，喜结连理，气运相牵，结盟之基由此立。」 |

---

## 六 · 英雄救美 `encounter.hero_saves_beauty`
> canon 锚：救美 = 好感+ / 结仇+ 双产出(同一事件同改两条关系)；男女主初遇模板。地点取江湖险地/坊市。归 Arc=Encounter。

### 1601 · 险地救困·结缘结仇（双产出，绑危险层 SiteHazards）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1601 / Encounter / 0 |
| Cooldown | 4000 / PerActor |
| BaseWeight / OncePerArc | 24 / false |
| Preconditions | `Self.atSiteKind == Self.atSiteKind`(同节点)；`Beauty.unifiedTier < Self.unifiedTier`(受困者弱于救者)；`Beauty.relTo(Foe) ≤ -30 或 SiteHazards(node).Intensity ≥ 30`(被调戏/凶险困住)；`IsAlive(Beauty)` |
| Effects | `AdjRelation(Self↔Beauty, +35)`(结缘)；`FormGrudge(Foe→Self, Insult, 35)`(救美得罪调戏者，**结仇**)；`AdjRelation(Self↔Foe, -30)`；`AdjFortuneSelf(Self, +8)`(侠名→气运微涨) |
| Chronicle | 「{Self} 路见{Beauty} 受{Foe}所困/陷于{atRegion}险地，仗义出手，美人垂青，恶徒衔恨。」 |
| 备注 | canon"救美=好感+/结仇+双产出，同一事件同时改两条关系"直译。Foe 缺槽则仅结缘(优雅降级)。 |

### 1602 · 漕帮护道·惊鸿一瞥（帮派向，绑 Gang Faction，气运+人脉）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1602 / Encounter / 0 |
| Cooldown | 5000 / PerActor |
| BaseWeight / OncePerArc | 20 / false |
| Preconditions | `Self.atSiteKind == Market 或 渡口`；`RivalSect.Type == Gang`(帮派劫掠)；`Beauty.unifiedTier < Self.unifiedTier`；`Self.unifiedTier ≥ 2` |
| Effects | `AdjRelation(Self↔Beauty, +25)`；`FormGrudge(RivalSect 代表→Self, Insult, 30)`(坏帮派好事)；`AdjReputation(Self.Sect, +5)` 若有 Sect；`AdjFortuneSelf(Self, +6)` |
| Chronicle | 「坊市渡口，{RivalSect}漕帮劫道，{Self} 拔刀护下{Beauty}，惊鸿一瞥，帮众记恨。」 |

---

## 七 · 门派大比/武林大会 `event.grand_tournament`
> canon 锚：戏剧 storylet 106"门派大比(批量切磋)"；周期性全局排位赛，争夺标的(秘籍/名号)，产全局排名/称号 Buff。绑 Faction(势力代表参赛) + 道枢裂痕(争夺加剧)。归 Arc=Encounter，多角色参与。

### 1701 · 论剑大会·剑墟主办（剑修/通用，名号+气运，绑剑墟道盟）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1701 / Encounter / 0 |
| Cooldown | 12000 / Global（**周期性全局事件，对齐 CyclePeriod**） |
| BaseWeight / OncePerArc | 28 / true |
| Preconditions | `Self.unifiedTier ≥ 5`(顶尖战力才有资格)；`World.ambientQi ≤ 500`(灵机下行→争夺烈)；`Self.atRegion == 东海·剑墟` 或可达；并发参与角色 ≥ 2 |
| Effects | 批量 `SparAction` 多角色对战(轮次制，确定性按 Id 升序配对)；胜者 `AdjFortuneSelf(+30)` + `SetFlag(title_论剑魁首, 1)`(称号 Buff)；胜者 Sect `AdjFactionFortune(+20)` + `AdjReputation(+15)`；败者 `AdjRelation(对胜者, -5)`(切磋微损，非世仇) |
| Chronicle | 「承熹某年，剑墟道盟主东海论剑，群雄毕至；{Self} 力压群豪，得『论剑魁首』之名，宗门气运随之鼎盛。」 |
| 备注 | 全局排位赛：胜者得称号+宝物+气运，宗门气运联动(可触发 Flourishing)。败者只 -5 关系(SparAction 日常切磋量级)，不形成 Grudge。 |

### 1702 · 镇玄司·御前演武（朝廷向，绑大胤朝镇玄司，站队/忌惮钩）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1702 / Encounter / 0 |
| Cooldown | 12000 / Global |
| BaseWeight / OncePerArc | 22 / true |
| Preconditions | `RivalSect == 大胤朝(镇玄司)`(朝廷主办)；`Self.Sect != null`；`Self.unifiedTier ≥ 6`；`Self.atRegion == 中州·神京` 或可达 |
| Effects | 批量 `SparAction`；胜者 `AdjReputation(Self.Sect, +20)` + `AdjFactionFortune(+15)`；**忌惮钩**：若 `Self.Sect.MightCache ≥ RegimeWarinessThreshold` → `AdjRelation(大胤朝→Self.Sect, -15 RegimeWariness)`(功高震主→朝廷猜忌，canon §4.7 忌惮链)；可埋 Vassal 投名状 storylet 种子 |
| Chronicle | 「大胤朝镇玄司设御前演武，江湖巨擘列席；{Self} 锋芒毕露，扬名朝堂，然功高震主，镇玄司暗生忌惮。」 |
| 备注 | 落地 canon"朝廷 vs 剑墟道盟忌惮链：MightCache 超阈→RegimeWariness 上升→打压/围剿"。可链式 SpawnArc(围剿弧)。 |

---

## 八 · 走火入魔 `mishap.qi_deviation`
> canon 锚：12 路特色失败(剑=剑走偏锋/魂=识海崩裂/鬼=鬼兵反噬/命=Karma 雪崩…)；`innerDemon≥80` 触走火打折(`DeviationDebuff` flag，不扣四维)；高风险高回报赌博。绑修炼资源 + 道心轴。归 Arc=Encounter（自发涌现于修炼轨迹，innerDemon 高时触发）。

### 1801 · 逆修剑走偏锋·剑意暴走（剑修向，战力↑理智↓双面，绑剑修走火锚）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1801 / Encounter / 0 |
| Cooldown | 4000 / PerActor |
| BaseWeight / OncePerArc | 24 / false |
| Preconditions | `Self.pathId == sword_immortal`；`Self.innerDemon ≥ 65`；`Self.res:swordWill == 0 或 在战中剑意空`；`Self.daoHeart < 50`(道心不稳) |
| Effects | 双面：`AdjResource(swordWill, +40)`(剑意暴涨→短期战力↑) **但** `SetFlag(DeviationDebuff, 1)`(走火打折，战力惩罚经 EffectOp，不扣四维)；`AdjResource(innerDemon, +15)`；`AdjResource(daoHeart, -8)`；小概率(dramaRng) `AdjStat(Constitution, -2)`(经脉受损) |
| Chronicle | 「{Self} 急于求进，剑意空时强催斩道，剑走偏锋，威能暴涨却神志渐失，宛若魔剑。」 |
| 备注 | 高风险高回报赌博：战力短涨 + 失控隐患。可自救：禅定/参悟降 innerDemon(走 1303 论道或闭关 Steady)。 |

### 1802 · 魂修探查反震·识海崩裂（魂修向，绑魂修最高反噬锚）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1802 / Encounter / 0 |
| Cooldown | 4000 / PerActor |
| BaseWeight / OncePerArc | 22 / false |
| Preconditions | `Self.pathId == soul_divine_sense`；`Self.res:seaIntegrity ≤ 30`(识海已损)；`Self.innerDemon ≥ 70`；`Self.unifiedTier ≥ 3` |
| Effects | `AdjResource(seaIntegrity, -25)`(识海反震，钳≥10 自救底线)；`AdjResource(innerDemon, +12)`；`SetFlag(DeviationDebuff, 1)`；若 `seaIntegrity` 触 10 → 强制自救 `AdjResource(daoHeart, -5)`(钳识海=10 保命) |
| Chronicle | 「{Self} 神识外探凶物，反震识海，分魂险溃；强凝识海于一线，未崩而已重创。」 |
| 备注 | 对齐魂修"reverbBacklash+5(本路最高)/识海崩裂自救钳 seaIntegrity=10"。 |

### 1803 · 鬼修噬主·鬼兵反噬（鬼修向，绑鬼修 devourMeter 跨阈锚）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1803 / Encounter / 0 |
| Cooldown | 4000 / PerActor |
| BaseWeight / OncePerArc | 22 / false |
| Preconditions | `Self.pathId == gui_xiu_yang_hun`；`Self.res:devourMeter ≥ DevourThreshold`(噬主度跨阈)；`Self.innerDemon ≥ 70`；`Self.flag:hasBrakeFamily == 0`(未配镇魂/献祭 brake 则更危) |
| Effects | `AdjResource(ghostSoldierPower, -30)`(鬼兵反噬主体)；`AdjResource(devourMeter, -20)`；`AdjStat(Constitution, -2)`；`AdjResource(innerDemon, +15)`；可自救 `GrantArt(gx_zhenhun 镇魂)` 若触发(brake) |
| Chronicle | 「{Self} 养鬼噬魂，devour 度满溢失控，所御鬼兵反噬其身；急施镇魂，方免魂消。」 |
| 备注 | 对齐鬼修"devourMeterOverflow+5/强制选1个 brake family(镇魂/献祭)"。 |

### 1804 · 命修夺运反噬·因果雪崩（命修向，绑命修 Karma>Fortune 锚）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1804 / Encounter / 0 |
| Cooldown | 5000 / PerActor |
| BaseWeight / OncePerArc | 20 / false |
| Preconditions | `Self.pathId == ming_fate_causality`；`Self.res:Karma > Self.res:Fortune`(净气运为负→反噬)；`Self.res:LifespanDebt ≥ 阈`；`Self.innerDemon ≥ 60` |
| Effects | `AdjResource(Karma, +15)`(因果反扑)；`AdjFortuneSelf(-20)`；`AddLifespanBonus(-10)`(透支折寿，进 ThreatPenalty)；`SetFlag(DeviationDebuff, 1)`；自救 `AdjResource(Karma, -10)`(因果落子清 Karma 降 ID) 若 `daoHeart ≥ 50` |
| Chronicle | 「{Self} 强夺命大者之运，Karma 倒灌，反噬其身，气运雪崩，寿元暗损；唯落子因果，可解一二。」 |
| 备注 | 对齐命修"karmaOverflow(每点差+1)/LifespanDebt 透支寿尽/自救=因果落子"。 |

---

## 九 · 闭关突破 `growth.seclusion_breakthrough`
> canon 锚：闭关 = 单点 wake + 中途不 Tick + 出关一次性补 AgeCost(A3-FINAL §2)；收益 = WorkUnits 推进 + BreakAid；闭关递增边际成本(折寿/心魔/顿悟权重递减，破避险刷点)；闭关期被 spar 短路 no-op+Disturb++。这些是**闭关期可点火的 storylet**（Epiphany/BottleneckLoosen 仍可点火但权重随 streak 递减）。归 Arc=Encounter。突破渡劫 roll 走 cult5；storylet 调度走 drama6。

### 1901 · 闭关顿悟·瓶颈松动（通用，破障+小境界，闭关期点火，权重随 streak 递减）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1901 / Encounter / 0 |
| Cooldown | 3000 / PerActor |
| BaseWeight / OncePerArc | 26 / false（**实际权重 ×(100−secludeStreak×10)/100，下限0：闭关顿悟边际递减**） |
| Preconditions | `Self.flag:secluded == 1`(闭关中)；`Self.phase == Bottleneck`；`Self.comprehension ≥ 10`；`Self.daoHeart ≥ 30` |
| Effects | `AddBreakProgress(+25)`(硬钳≤gain×3)；`AddBreakAid(+min(secludeStreak×4, 30))`(对齐"闭关连续K→BreakAid")；`AdjResource(comprehension, -5)`(顿悟消耗)；递增成本 `AdjResource(innerDemon, +secludeStreak×3)`(枯坐生心魔) |
| Chronicle | 「{Self} 枯坐闭关，于死寂中忽得灵光，瓶颈松动；然闭关日久，心魔暗滋。」 |
| 备注 | INV-NO-DOMINANT 兜底"一直闭关"非占优。闭关期奇遇暴露=0(碰不到外缘)，仅此类纯收益+递增成本可点火。 |

### 1902 · 出关渡劫·斩道破境（剑修/通用，realm 边界评劫，cult5 roll）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 1902 / Encounter / 0 |
| Cooldown | 6000 / PerActor |
| BaseWeight / OncePerArc | 30 / true |
| Preconditions | `Self.flag:seclusionResolve == 1`(出关结算时刻)；`Self.res:<主资源> ≥ SubLevel 边界 work`；`Self.unifiedTier ≥ 1` |
| Effects | 走 v1.2 渡劫机评劫(占 1 次 `cultRng.Split(SECLUSION_STREAM^EnterClock)`)：`TribScore = Σ(ResistTerms) − ThreatPenalty + BoundedRoll`；过则 `RealmDelta(+1)`，否则 `AdjResource(<主资源>, -损耗)`(失败损资源不掉境)；出关 `AddLifespanBonus(-AgeCost)`(单点计入，禁双计) |
| Chronicle | 「{Self} 出关之日，引动天劫/斩道破壁；一线之间，或登新境，或铩羽而归，岁月已逝。」 |
| 备注 | 渡劫 roll 归 cult5(归 CultPump，RuleBrain 零改)。AgeCost 唯一计入出关单点(中途不 Tick 物理保证)。 |

---

## 十 · 恩怨偶遇 `encounter.fated_meeting`
> canon 锚：戏剧 storylet 102"恩怨偶遇(同节点→若 Hunting 推进 Showdown，否则关系恶化加码)"；从已有关系池抽对象，按关系值(仇/友/欲夺宝)决定 战斗/交易/同行分支。归 Arc=Revenge(Hunting) 或 Encounter。被动涌现于行路轨迹。

### 11001 · 江湖偶遇·旧仇狭路（恩怨偶遇主节点，关系驱动多分支）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 11001 / Revenge / Hunting |
| Cooldown | 2000 / PerPair |
| BaseWeight / OncePerArc | 28 / false |
| Preconditions | `Self.atSiteKind == Foe.atSiteKind && Self.atRegion == Foe.atRegion`(同节点)；`grudgeIntensity(Self,Foe) ≥ 30`；`IsAlive(Foe)` |
| Effects | 关系分支(dramaRng)：① 若 `grudgeIntensity ≥ GrudgeIgniteThreshold` 且弧在 Hunting → 推进 Showdown(喂 1403)；② 否则 `AdjGrudge(Self→Foe, +10)`(关系恶化加码) + `AdjRelation(-15)` |
| Chronicle | 「{Self} 行至{atRegion}，狭路撞见旧怨{Foe}；新仇旧恨翻涌，或当场了断，或暗记一笔。」 |
| 备注 | 直译戏剧 storylet 102。从已有 GrudgeLedger 关系池抽对象。 |

### 11002 · 觊觎者同行·欲夺宝（怀璧偶遇，欲夺宝分支，绑 holdsRiftShard）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 11002 / Encounter / 0 |
| Cooldown | 3000 / PerPair |
| BaseWeight / OncePerArc | 22 / false |
| Preconditions | `Self.flag:holdsRiftShard == 1 或 Self.res:itemTier ≥ 6`(怀璧)；`Foe.atRegion == Self.atRegion`；`Foe.ambition ≥ 50`；`relTo(Self,Foe) ∈ [-30, 30]`(尚未撕破脸) |
| Effects | 分支(dramaRng)：① 交易 → `TransferFortune` 双向(以物易物，守恒) + `AdjRelation(+10)`；② 欲夺宝 → `FormGrudge(Foe→Self, Maiming, 45)` + `AdjRelation(-25)` + `SpawnArc(Revenge)` 若并发未满 |
| Chronicle | 「{Self} 怀宝行于江湖，{Foe} 笑脸相迎；或互通有无结善缘，或图穷匕见欲夺宝。」 |

### 11003 · 同道偶遇·结盟同行（友好偶遇分支，关系池正向，气运/人脉）
| 字段 | 值 |
|---|---|
| Id / Arc / Stage | 11003 / Encounter / 0 |
| Cooldown | 3000 / PerPair |
| BaseWeight / OncePerArc | 18 / false |
| Preconditions | `Foe.atRegion == Self.atRegion`(此处 Foe 槽实为旧识)；`relTo(Self,Foe) ≥ 40`(关系池正向→旧友)；`|Self.unifiedTier − Foe.unifiedTier| ≤ 2`(实力相当)；同 `AlignmentAxis` |
| Effects | `AdjRelation(Self↔Foe, +15)`；`AddBreakAid(+5)`(切磋互助)；若双方有 Sect 且 Sect 同轴 → `AdjRelation(两 Sect, +8)`(私谊带动势力) |
| Chronicle | 「{Self} 途遇故交{Foe}，把酒言欢，结伴同行，互参武学，气运相济。」 |
| 备注 | canon"按关系值决定 战斗/交易/同行分支"之同行分支。从关系池正向抽对象。 |

---

## 附录 · 整数初值 + cap 落点（per-storylet 可覆盖，标定期收敛）

```
点火节流:    EncounterCheckInterval=5 (drama Pump 节流，非每 Advance 全扫)
全局并发:    MaxConcurrentArcs=3; MaxArcsPerCharacter=1 (一人一时一条主线弧)
奇遇频率cap: GlobalCap=max(12, AliveCount×PerActorTarget(4)) (按人口缩放保下界可达)
            CatCap=4/100tick (per-category) ; ActorMinGap=30 (per-actor 硬上界)
            ROAM_ENCOUNTER_MUL=3 (只提概率/权重，不绕 ActorMinGap)
强度门:      GrudgeIgniteThreshold (≥才点火复仇) ; MassacreGap (灭门碾压差)
收益护栏:    ENCOUNTER_PROGRESS_RATIO=3 (单次奇遇 progress ≤ gain×3 硬钳)
            (progress+breakProgress+breakAid折算) 奇遇占比 ≤ 35%
            LEGACY_LIFESPAN_MAX=200 (lifespanBonus OncePerLife 单次小额)
            BREAK_AID_CAP (BreakAid 钳)
salience:    同 Category 连命中 → 权重 ×(100−recentHits×20)/100 (防同套路疲劳)
闭关递减:    1901 实际权重 ×(100−secludeStreak×10)/100 (闭关顿悟边际递减)
内容池:      POOL_MIN≈60 (本库 22 条为竖切骨架，扩到 POOL_MIN 是 plan 硬目标)
            INV-VARIETY-CONTENT: 任意200tick窗口 distinct/总点火 ≥ DIVERSITY_MIN
RNG:        点火主流串行消费 dramaRng(Split6) ; 弧内 dramaRng.Split(arcId) 子流(顺序无关)
            修炼资源 roll(渡劫/Epiphany) 走 cultRng(Split5) 经 ApplyResource
全局稀缺律:  ① 灌顶/神功类(1302) 全局限量 Cooldown=Global ; ② 怀璧其罪(1102→1402/11002) 物品自动生成觊觎者
守恒律:      气运 Transfer 源减目标增(TransferFortune) ; 碎片气运来自全局未分配池非凭空增发
```

## 对齐已锁系统核对（无冲突）
- **12 路**：per-path storylet 经 `Self.pathId` 谓词门控 + `res:<key>` 路由(swordWill/seaIntegrity/itemTier/Fortune/devourMeter/vow…)，资源全经 `ApplyResource` chokepoint；功法名取 12 路深度(独孤九剑·破剑式/身外化身/大梵般若/落宝夺器纹…)；通用 storylet 按 pathId 路由主资源，零路线偏袒。
- **三轴境界**：门控/效果只读 `unifiedTier`(UT0-12 跨路刻度)与 `realmIndex`；`RealmDelta` 极罕见且门控最严(仅 1302 灌顶/1902 渡劫)，不碰 flatIndex/小境界编码。
- **8 Faction**：势力槽经 `Self.Sect/RivalSect.Type`(Sect/Court/Clan/Demonic/Gang…)谓词；气运经 `Faction.ApplyFortune`，关系经 `Relations.Adjust`([-100,100])；正邪是中性 `AlignmentAxis` 标签。
- **RngStreamIds**：storylet 调度/点火/分支走 **drama=Split6**(对齐戏剧 storylet 机制)；修炼资源 roll 走 cult=Split5；运行期消费的流升 World 字段进 Clone(INV-RNG-PERSIST)。
- **侧表纪律**：零进 v1.0 核心 record(StatBlock/Character/Persona/Sect/WorldNode)；新态全挂 CultivationState.Resources/GrudgeLedger/DramaProfile/Faction 侧表。
- **软克制**：1403/1501/1701 等战斗复用 `SparAction` 五段式(境界压制×per-path+情境±P0/4+战技+有界roll)，黑哨/主场=情境 adj 在 ±25% 闸内，**无 path-vs-path 硬克制环**。
- **不锁死(L0)**：加一条 storylet = 追加 `IStorylet` 数据行，零改 director/引擎；新功法/资源/势力经 string key 软绑。

---

# 第五部 · 命名生成池库

# 九野·确定性命名生成池库（NamingPoolLibrary · NPL-v1）

> 任务库：命名生成池库。产出「九野」世界（江湖纪·大胤承熹·末劫将临）专属的确定性命名生成池——驱动势力/角色/功法/地名的具名生成。一切名号严格归属九野 canon，可程序化（整数+随机有上限），不锁死（加成分=加 L0 数据行）。
>
> **canon 锚定**：本库取自 `2026-06-13-WorldBible-九野-canonical.md` 与 `2026-06-13-v1.2-A-修炼路线-每路深度设计.md`。功法名词根中**所有具名功法/战技/境界**均直接抄录自 12 路深度设计原文（非杜撰），故生成的功法名与已锁 ArtDef/CombatSkillDef 内容库严格同源；势力/地名锚点对齐 §4.7 / §5.2 的六大初始势力与命名大区。

---

## 0 · 库定位、与已锁系统的接缝、确定性契约（先钉死，后续池只引用）

### 0.1 这是什么、不是什么
- **是**：一套**生成期（gen 流）纯数据名号成分表 + 确定性整数组合规则**。产出的全部是 **flavor 字符串**（`FactionDef.Name` / `Persona`-侧道号 / `ArtDef.Name` / `LandmarkDef.Name` / Chronicle 文本），**零数值路径**。
- **不是**：机制。境界/战力/12 路分轴/三轴解耦/8 Faction 类型/关系量纲/RngStreamIds——全部由 World Bible 已锁，本库**只命名、不定义机制**。功法名词根分组**复用** 12 路深度设计的 `pathId` 与具名功法清单；境界词根**复用** §3.2 力量层级总览表（仙侠/武侠双锚称谓）与各路 `realmMultipliers` 描述里的境界投影。

### 0.2 RNG 流归属（承 canonical §8.2 / §8.3，append-only 不开新顶层号）
- **命名全程走 `genRng=Split(1)` 的命名子流**，新登记一个命名子流号 **`GEN_NAMING=102`**（与已有 `GEN_HISTORY=101` 并列，均属 Split(1) 生成期；`Pcg32.Split` 跳号派生不消费 root 状态，`Pcg32.cs:58`）。
- **生成期一次性消费**：命名在 `WorldFactory.CreateInitial` 生成期（势力/角色/地图/历史 Seed 阶段）一次性产出并写入既有 flavor 字段；**运行期不再消费命名流**。故命名流**不需**升格为 World 字段进 Clone（区别于 map7/faction8/cult5——那是运行期消费才需进 Clone，承 INV-RNG-PERSIST）。命名是「初始条件的名字」，与「冻结的过去」同相。
- **off 纪律**：naming-off（或上游 faction/geo/cult off）时不构造命名池对象、不调 `Split(102)`，既有 `Split(1..4)` 派生顺序与消费次数不变 → INV-OFF-38 逐字节不变。
- **进一步分流（防串号、保顺序无关）**：命名子流再按域 `Split`：
  ```
  GEN_NAMING=102
    .Split(NS_FACTION=1)   // 势力名
    .Split(NS_PERSON=2)    // 人名(姓+名)
    .Split(NS_DAOHAO=3)    // 修士道号
    .Split(NS_ART=4)       // 功法名(再 .Split(pathOrdinal) 派生每路独立流)
    .Split(NS_PLACE=5)     // 地名/地标
  // 每域子流顺序无关(Split 跳号派生)，加新域=加新 NS 常量(L0)
  ```

### 0.3 确定性组合规则（所有池共用的「种子→索引→取词」铁律）
- **基元抽词**：`pick(pool, rng) = pool[ rng.NextInclusive(0, pool.Count-1) ]`，纯整数索引、向下、可复现。**先 clamp 索引到 `[0,Count-1]` 再取**（硬上限优先于随机，对齐 §5.3 / §7.4「随机有上限」）。
- **加权抽词（可选偏置）**：池行可带 `int Weight`（默认 10）。`pickWeighted(pool, rng)` = 前缀和 + 整数轮盘（**复用** §7.4 `WeightedPicker`；同种子逐字节一致）。裁决前池按 `(Weight desc, RowId asc)` 稳定排序，**禁 Dictionary/HashSet 枚举序参与裁决**（承 §7.4 纪律）。
- **去重纪律**：同一实体（同一势力/角色）一次组合内，同一槽位重复抽到已用值则**线性探测下一行**（`idx=(idx+1)%Count`），对齐项目「最少使用去重」同构。全局唯一名（地标/顶级势力）用 `usedNames:HashSet<string>` 在生成期过滤，撞名则重抽至上限 `MaxNameRetry=8`，超限回退到「`基名+序号`」兜底（不静默崩）。
- **频率/强度/并发上限**（承「随机但有上限」三类门控）：
  - 频率：每类名号的随机微成分（如随机世家/帮派）受上游已锁上限约束（势力数、Site 数、`MaxSecrets` 等），命名不新增实体只贴名。
  - 强度：「品阶词」「尊号词」按 realm/UnifiedTier/势力 rank **门控**（低境不得取「祖师/天尊」「至宝」级词，见各池规则），防名号通胀。
  - 并发：复姓/冷僻尊号设 `RareWeight`（默认 2，远低于常见 10），自然压低稀有名密度。
- **不锁死（L0）**：每池都是 `List<NameRow>`，**加名 = 追加一行数据**；加新成分类别 = 加一张表 + 加一个 NS 子流常量。引擎读表零特例。新增路线的功法名只需在 `ArtNameBank` 加一个 `pathId` 分组（L0），不改组合器。

### 0.4 与四维/侧表纪律的硬隔离
- 命名**绝不进 `StatBlock` 4 维**、**绝不进 Σ=80**、**绝不进任何 `EffectivePower`/`TribScore`**。名号只写：`FactionDef.Name`、`Persona.Origin`/角色显示名（经 `with` 改值不改 schema，承 §7.3 / §8.1）、`ArtDef.Name`/`CombatSkillDef.Name`（flavor 名只进 Name/Note，承深度设计 §1.3）、`LandmarkDef.Name`/`RegionDef.Name`。

### 0.5 字义/性质标签（让组合受 canon 约束而非乱配）
每池行可挂标签位，组合器据此**约束抽取**（不是装饰，是 guard）：
- `Align ∈ {正+1, 中0, 邪-1}`（对齐 §4.2 `AlignmentAxis`）。
- `FactionType ∈ {Sect,Court,Clan,Demonic,Gang,Merchant,Rogue,Exotic}`（§4.2）。
- `Element ∈ {金,木,水,火,土,纯阳,阴煞,无}`（对齐 §5.2 `ElementAffinity` + 软情境元素轴）。
- `PathId ∈ 12 路 pathId`（功法名/道号偏好绑路）。
- `Register ∈ {雅逸, 刚烈, 阴诡, 佛禅, 道玄, 庙堂, 商俗, 蛮异}`（语域，决定哪条姓/字/后缀可与哪类势力相配）。

---

## 1 · 姓氏池（SurnamePool）—— 驱动角色姓、世家名、师承谱系

> 用于：角色姓（人名 = 姓+名）；世家型 Faction 冠姓（如「轩辕氏」「慕容世家」）；血脉/师承谱系根（§7 Bloodline 锚点）。

### 1.1 单姓池 `SinglesSurname`（常见，Weight 默认 10）
古雅清越向（与江湖纪雅逸语域相配）：
```
叶 慕 沈 林 苏 陆 萧 楚 姜 卫 秦 谢 裴 江 温 封 顾 程 柳 池 商 卓 燕 容 凌 洛 宋 莫 关 simoned→删 余 方 罗 钟 时 应 厉 言
```
（共 39 行示例；`池/商/凌/洛/莫` 等兼可作地名/门派字根，跨表共享。）

### 1.2 复姓池 `CompoundSurname`（显门第，RareWeight 默认 3；自带「世家/古族」门第标签）
```
令狐 慕容 公孙 独孤 东方 西门 上官 欧阳 端木 夏侯 诸葛 皇甫 司徒 司马 轩辕 南宫 北堂 长孙 宇文 慕白
```
- **标签**：复姓行默认 `Register=雅逸|庙堂`，可挂 `FactionTypeHint=Clan`（抽到复姓的角色更可能锚到世家型势力，对齐 §4.2 世家「血脉传承」）。`轩辕` 直接对应 §1 考据徽山轩辕世家范式，挂 `Align=中0`。
- **强度门控**：复姓仅在「世家子弟 / 名门之后 / 隐藏血脉（§7 Bloodline）」角色生成时进池；散修/市井凡人默认只抽单姓（防复姓滥用稀释门第感）。

### 1.3 组合规则
```
姓 = (角色 isCompoundEligible && subRng.NextInt(100) < CompoundChance(默认15))
       ? pickWeighted(CompoundSurname, subRng)
       : pickWeighted(SinglesSurname, subRng)
// isCompoundEligible 由上游身份(世家籍/血脉锚)给出；CompoundChance 进 LimitsConfig.NamingConfig
```

---

## 2 · 男名字池（MaleGivenPool）—— 与姓拼成男性角色名

> 构词：`名 = 姓 + 字`，字可单字或双字（`DoubleChance` 默认 25 → 抽两字拼接，去重避叠字）。

### 2.1 刚烈路 `MaleGiven_Fierce`（武夫/剑修/雷修/魔道偏好，Register=刚烈）
```
锋 钧 烈 岳 霆 野 破 天 麟 渊 戈 锐 霄 罡 烬 戟 镔 acked→删 啸 镇 砺 崭 钺 烈 暴 雄 彪 猛 钢 铁 轰
```

### 2.2 清逸路 `MaleGiven_Refined`（法修/丹修/书香/正道文士偏好，Register=雅逸|道玄）
```
之 羽 玄 清 远 书 白 竹 砚 朗 川 行 怀 默 谦 修 简 澈 旷 樗 璞 钰 珩 珏 暄 霁 屹 翊 衍 昭
```

### 2.3 字义典故位 `MaleGiven_Themed`（可选，挂字义标签，对齐金庸「单字+义理」范式）
```
靖(家国/靖难) 过(改过) 破虏(家国) 守拙(谦隐) 问天(问道) 承熹(扣大胤年号·宗室子弟专用) 镇玄(扣镇玄司·官身专用) 砥砺 慎独 抱朴(道玄) 见独 守一(道玄)
```
- **强度门控**：`承熹/镇玄` 只对朝廷宗室/镇玄司官身角色开放（扣 §2.1「大胤·承熹·镇玄司」canon，防滥用国号）。

### 2.4 组合规则
```
register = pathRegisterOf(角色 PathId, FactionType)  // 剑/雷/魔→Fierce偏置; 法/丹/佛/正道文→Refined偏置; 庙堂→可入Themed
pool = (subRng.NextInt(100)<ThemedChance(默认8) && themedEligible) ? MaleGiven_Themed
     : register==刚烈 ? MaleGiven_Fierce : MaleGiven_Refined
字 = subRng.NextInt(100)<DoubleChance ? (pick(pool)+pick(pool 去重)) : pick(pool)
```

---

## 3 · 女名字池（FemaleGivenPool）—— 与姓拼成女性角色名

> 构词同男名；女名偏「双字叠意」结构（`DoubleChance` 默认 45，高于男名）。

### 3.1 柔雅意象 `FemaleGiven_Grace`（Register=雅逸）
```
烟 霜 月 蘅 嫣 绾 凝 纨 瑶 婉 盈 雪 茜 黛 妸 姝 棠 荥→删 漪 鸢 璎 颦 莞 鸾 婵(扣考据鬼灵门少主王婵范式) 妧 笙 沅 泠
```

### 3.2 植物香药 `FemaleGiven_Herb`（Register=雅逸；丹修/医毒/合欢式势力女修偏好）
```
蘅 芷 萝 茵 砂(朱砂/辰砂·贞洁) 苓 薇 蓉 蕊 菡 苕 荇 蘅芜 杜若 灵素(扣考据程灵素) 青黛 紫苏 半夏 曼陀
```
- **半夏/曼陀/紫苏** 挂 `Element=阴煞|毒`、`FactionTypeHint=Demonic`（毒/蛊类邪宗女修偏好，对齐南疆百蛊渊/合欢式势力）。

### 3.3 组合规则
```
pool = (毒蛊/邪宗女修) ? mix(FemaleGiven_Herb 偏置, FemaleGiven_Grace)
                       : mix(FemaleGiven_Grace 偏置, FemaleGiven_Herb)
名 = subRng.NextInt(100)<DoubleChance ? 双字叠意(pick+pick 去重) : pick(pool)
```

---

## 4 · 修士道号词根（DaoHaoPool）—— 修士/宗门长老/异人的道名+尊号

> 承 §3.1：修士（Cultivator，`CanAscend=true`）入道另取道号，**与俗名并存**（俗名走姓+字，道号是 Persona 侧 flavor）。构词：`道号 = [意境词根] + [尊号后缀]`。

### 4.1 意境词根 `DaoHao_Theme`（Register=道玄|佛禅，可挂 PathId/Element 偏好）
```
纯阳(纯阳/雷修偏好) 紫阳 清虚 玄真 抱朴 无尘 太乙 冲虚 玄微 守拙 通玄 鸿蒙 混元 太初 青冥 紫霄 无量 寂灭(佛禅) 明心(佛禅) 离尘(佛禅) 幽冥(鬼修/阴煞) 噬煞(鬼修) 玄阴(鬼修) 大衍(命修/魂修·扣大衍诀) 推命(命修) 御火(丹修) 丹元(丹修) 万器(器修) 御灵(驭兽) 牧云(驭兽)
```
- 词根**绑路偏好**（非硬绑）：`纯阳/雷` 偏雷修、`幽冥/噬煞/玄阴` 偏鬼修（且 `Align=邪-1`）、`大衍/推命` 偏命修魂修、`寂灭/明心/离尘` 偏佛修、`御火/丹元` 偏丹修。抽道号时若角色有 PathId，先从该路偏好词根加权，再回退通用词根。

### 4.2 尊号后缀 `DaoHao_Honorific`（**强度门控**：按 UnifiedTier/势力 rank 升序解锁，防低境僭称）
```
RankBand 0  道人 / 山人 / 散人        (UT 0-2，记名~外门)
RankBand 1  炼师 / 居士 / 先生        (UT 2-4)
RankBand 2  真人 / 上人               (UT 4-6，化神/宗师段)
RankBand 3  宗师 / 真君               (UT 6-8)
RankBand 4  老祖 / 天师               (UT 8-9，陆地神仙/掌门段)
RankBand 5  天尊 / 道君               (UT 10+，渡劫/飞升传说段·全局稀缺)
```
- 后缀**门控公式**：`maxBand = clamp(UnifiedTier/2, 0, 5)`，`pick(DaoHao_Honorific where Band<=maxBand)`。对齐考据「道人<炼师<真人<宗师<天师<祖师/天尊」阶梯 + §3.2 UnifiedTier 投影。`天尊/道君` 仅 UT≥10（渡劫/飞升态）可取，天然稀缺（飞升即离场，§3.2）。

### 4.3 组合规则
```
theme = 角色有PathId ? pickWeighted(DaoHao_Theme where PathPref==PathId 优先) : pickWeighted(DaoHao_Theme)
hon   = pick(DaoHao_Honorific where Band <= clamp(UT/2,0,5))
道号  = theme + hon                       // 例: 纯阳+真人=「纯阳真人」; 幽冥+老祖=「幽冥老祖」; 大衍+道君=「大衍道君」
// 邪修(Align=-1)道号偏好阴诡词根+可降一档尊号(魔道不慕正统虚名)
```

---

## 5 · 门派名词根（FactionNamePool）—— 8 类 Faction 命名

> 承 §4.2（8 类型位）+ §4.7（六大初始势力锚点）+ §5.2（地标耦合）。构词：`势力名 = [前缀] + [核心字根] + [宗门后缀]`，前缀可空。**类型/立场约束后缀**（武力型→剑阁/拳宗；隐修→谷/观/庄；邪派→教/血刀门；朝廷→司/朝/庭；商会→商会/钱庄；异族→族/部）。

### 5.1 前缀词根 `FactionPrefix`（方位 / 天象，可挂 Element/Align）
```
方位: 北 南 东 西 中州 塞外 苗疆 东海 北漠 西陲 江南    (对齐 §5.2 命名大区)
天象: 天 玄 紫 青 碧 金乌 北冥 太乙 凌霄 日月 紫霄 玄黄
```

### 5.2 核心字根 `FactionCore`（山水 / 灵兽 / 道佛 / 武器，跨表与地名共享）
```
山水: 青岚 沧浪 云岫 灵鹫 桃花 蓬莱 昆仑 剑墟(扣§4.7剑墟道盟) 百蛊(扣§4.7百蛊渊) 万器(扣§4.7万器谷) 铁佛(扣§4.7铁佛寺) 落霞 通天 大竹 寒潭 听涛 melt→删 栖云 万峰
灵兽植物: 鹰爪 血刀 铁掌 合欢 百花 五毒 天鹰 龙虎 玄龟 朱雀 白泽 螣蛇 古蛊(扣§4.7苗疆古蛊一族)
道佛词: 青云 天音 焚香 真武 太清 上清 玉清 般若 金刚 净土 罗汉 玄都 紫府
武器器物: 巨剑 噬魂 番天 落宝 玲珑 镇岳 八灵
```

### 5.3 宗门后缀 `FactionSuffix`（**类型约束**：按 FactionType 选后缀子集）
```
Sect(正/邪宗门):  宗 门 派 阁 观 寺 谷 庄 峰 洞天 道盟(扣剑墟道盟)
Demonic(魔道):    教 魔宗 血刀门 鬼宗 蛊渊 邪宗 魔窟
Court(朝廷):      朝 庭 司(扣镇玄司) 王庭 行辕 都护府
Clan(世家):       世家 氏 府 山庄 堡 坞
Gang(帮派):       帮 会 寨 堂 门(绿林)
Merchant(商会):   商会 钱庄 镖局 行 拍卖行 货栈
Exotic(异族):     族 部 王庭 古族 教(巫教)
Rogue(散修):      —(伪势力, 用固定名「散修登记处」, 不组合)
```

### 5.4 组合规则（受 Type×Align 双轴约束，承 §4.2 正交）
```
suffixSet = FactionSuffix[Type]                      // 类型决定后缀子集
core      = pickWeighted(FactionCore where (Align匹配 || Align==中))
prefix    = subRng.NextInt(100)<PrefixChance(默认55) ? pick(FactionPrefix) : ""
suffix    = pick(suffixSet)
name      = prefix + core + suffix
// 立场微调: Align=邪 偏好 灵兽植物(五毒/合欢/血刀)+道佛词反用; Align=正 偏好 道佛词+山水
// 全局唯一: 六大初始势力(§4.7)用固定 canon 名直接锚, 不进随机组合; 程序生成世家/帮派/异族才组合
// 例: 青(prefix)+云(core)+宗(Sect正) =「青云宗」; 百蛊(core)+魔宗(Demonic邪)=「百蛊魔宗」;
//     北(prefix)+铁佛(core)+寺(Sect正)=「北铁佛寺」; 镇玄(core)+司(Court)=「镇玄司」
```
- **地标耦合**（§5.2）：势力名核心字根优先与其 `HomeRegion` 的地标名同源（剑墟道盟据「东海·剑墟」、百蛊渊魔宗据「南疆·百蛊渊」）——组合时若势力已绑 `LandmarkDef`，`core` 直接取地标核心词，保「名实相符」。

---

## 6 · 功法名词根（ArtNamePool）—— 按 12 路分组，意境词+体用词+品阶词

> 承深度设计：**每路有命名各异的功法类目**。本池**按 `pathId` 分组**，每组的「具名功法」**直接抄录 12 路深度设计原文**（已锁内容），故生成的功法名与 ArtDef/CombatSkillDef 严格同源；新功法 = 在对应路分组追加一行（L0）。
>
> 通用构词（对齐考据「修饰前缀+核心+类型后缀」）：`功法名 = [意境前缀] + [体用核心] + [品阶/数目后缀]`。但**优先直出 canon 具名功法**（下表 6.2 每路清单），构词器只用于「程序生成新功法变体」时的回退。

### 6.1 通用三段成分（回退生成用；每段可挂 PathId/Element 偏好）

**意境前缀 `ArtTheme`**（绑路偏好）：
```
剑(sword): 独孤 太上忘情 两仪 三尺霜 流光 万剑 人剑合一
体(ti_xiu): 金钟 铁布 十三太保 金刚不坏 不灭金身 易筋 洗髓 九转玄功 罗汉
法(fa_xiu): 赤焰 玄冰 紫雷 青木 五行 混元 万符 诛仙天书 缥缈
阵(array): 四象 乱石 五行生杀 九宫八卦 周天星斗 奇门遁甲 无相
器(qixiu): 百炼 庚金玄玉 万材归炉 玄天蜕器 万宝朝宗 人器合一 番天镇岳 混元玲珑
魂(soul): 大衍洞玄 阳神炼魂 九转洗魂 九幽噬魂 焚天灭魂 万魄锁神 夺舍重生
命(ming): 观星 周天卦象 紫微斗数 太乙神数 青冥 因果连珠 推天衍命 断人生死
丹(dan): 聚气 回元 破障筑基 九转金身 夺元化魂 文武相济 心火不乱 万古药藏 万火归炉帝焰
鬼(gui): 勾魂鬼音 七鬼噬魂 玄魂炼妖 阴罗刹 万鬼幡 鬼婴夺胎 阴煞结丹 九幽煞河 夺舍重煞
佛(buddhist): 般若波罗蜜 金刚般若 大日如来 无量寿 诸天梵唱 罗汉金身 不动明王 菩提无生 我不入地狱
雷(lei_xiu): 五雷正法 惊蛰十二变 五府雷印 辟邪神雷 灭世雷光 三十六雷 纯阳灭阴 雷帝真身 普化天雷
驭兽(yu_shou): 百兽听令 伏魔降兽 万兽朝宗 上古驭龙 血契缔结 万灵归一 百兽朝王 通灵化形
```

**体用核心后缀 `ArtBodyUse`**（决定武学类别字段，对齐考据「经·诀·决·功·典·册」+ 战技「剑/掌/拳/指/步/遁/术」）：
```
心法/功法类(内功): 真经 神功 心经 宝典 功 诀 决 典 册 经 法
剑刀类: 剑法 剑式 剑诀 剑意 剑典 刀经
拳掌指类: 拳 掌 指 爪 腿
身法轻功类: 步 微步 遁 身法 翅
术法符印类: 术 诀 符 箓 印 阵 大法
战技通用: 式 击 一剑 一击 自爆
```

**品阶/数目后缀 `ArtGradePrefix`**（嵌数字成招 / 缀品阶字；**强度门控**：高阶字按 art tier 解锁）：
```
数目: 九 三 六 十八 三十六 三千 万 五十  (例: 降龙十八掌式→九/十八/三十六)
品阶字(tier门控): 基础(t1) / 玄(t2) / 真(t3) / 绝·灵(t4) / 神·天·至(t5+)
凡黄玄地天(承考据品阶双轨, 给丹/器的器物品阶用): 凡 黄 玄 地 天
```

### 6.2 每路具名功法直出清单（canon 原文，按类目，生成时优先直出）

> 下列为各路 ArtCategory 下的**具名功法名**（直接取自深度设计），生成「某路某境界角色的功法配置」时按 `SelectionRuleDef` 直出。仅列功法**名**（数值/效果在已锁 ArtDef，不在本库）。

- **剑修 sword_immortal**
  - 剑法：三尺霜 / 流光剑法 / 独孤九剑·破剑式 / 两仪微尘剑 / 太上忘情·无量剑诀
  - 心法：凝剑诀 / 清心剑典 / 太清剑意心经 / 慈航剑典·静念禅 / 两仪剑心·太极归元
  - 身法：踏雪无痕 / 凌波微步 / 御剑术·乘虚 / 剑遁·三十六天罡 / 法天象地·万里御剑
  - 剑意：见血意·杀伐 / 守拙意·后发 / 剑冢意·万剑朝宗 / 斩道意·见微 / 本命剑意·人剑合一
  - 战技：剑二十三 / 万剑归宗 / 破·御剑诀 / 遇强破式 / 剑气如虹 / 舍身一剑 / 祭剑·见血
- **炼体 ti_xiu_hengshi**
  - 横练功：金钟罩 / 铁布衫 / 十三太保横练 / 金刚不坏体神功 / 不灭金身决
  - 拳脚功：罗汉拳 / 崩拳 / 铁砂掌 / 大力金刚指 / 霸王举鼎
  - 血气吐纳法：龟息吐纳 / 易筋经 / 洗髓经 / 九转玄功 / 大日如来掌·气血篇
  - 战技：横练护体·铁山靠 / 燃血狂攻 / 金身震 / 舍身撞 / 金钟落锁 / 不灭金身
- **法修 fa_xiu**
  - 法术：赤焰术·烈阳爆 / 玄冰锥·寒髓封 / 紫雷诀·九霄落 / 青木生发术·回春 / 五行倒乱·混元法
  - 符印：聚灵符 / 金光护身符 / 五行封灵阵 / 乾坤挪移符 / 万符归元·天书符篆
  - 心法：基础吐纳引气诀 / 两仪聚灵功 / 太虚神识录 / 诛仙天书·正卷心法 / 缥缈御灵·周天搬运
  - 战技：万剑诀·御物千刃 / 五雷正法 / 寒冰六合困 / 焚天燎原阵 / 五行大遁术 / 混元一气·万法归宗 / 灵犀御空
- **阵修 array_formation**
  - 阵图：四象聚灵阵 / 乱石迷踪阵 / 五行生杀阵 / 九宫八卦锁灵阵 / 周天星斗大阵
  - 阵纹：导灵纹 / 聚石纹 / 固基纹 / 借势纹 / 叠阵纹
  - 布阵心得：一气演阵诀 / 双手互搏布阵心法 / 奇门遁甲心得 / 周天算经 / 无相心阵
  - 战技：落阵·点枢 / 急布·简阵 / 挪阵·乾坤 / 困龙·锁 / 引爆·焚阵 / 算尽·叠杀 / 破阵·反制
- **器修 qixiu_artificer**
  - 炼器术：百炼基础锻器诀 / 地火淬锋法 / 庚金玄玉炼宝术 / 万材归炉大法 / 玄天蜕器神通
  - 器纹学：止戈缚锋纹 / 聚灵导力纹 / 落宝夺器纹 / 金身护宝纹 / 山河镇杀纹
  - 御宝心法：心宝相照诀 / 一气御三器法 / 人器相生大法 / 万宝朝宗诀 / 人器合一·手中无器
  - 藏宝阁(法器名)：寒铁飞针 / 红线遁光针 / 青竹蜂云剑 / 八灵尺 / 番天镇岳印 / 落宝金钱 / 混元玲珑塔
  - 战技：御剑斩 / 落宝金光 / 缚锋锁器 / 万宝齐发 / 至宝压境 / 玄黄护宝罡 / 器灵自爆
- **魂修 soul_divine_sense**
  - 神识：大衍洞玄诀 / 阳神炼魂经 / 九转洗魂诀 / 凝神astral静定法 / 守一养神术
  - 魂术：九幽噬魂大法 / 焚天灭魂指 / 万魄锁神咒 / 摄魂夺魄术 / 神念刺
  - 秘术：夺舍重生大法 / 分魂化念秘术 / 归元守识秘法 / 神识探查·望气 / 魂烬反噬引
  - 战技：神识奇袭·夺魂一击 / 万魂幡·群体摄神 / 焚魂自爆·阳神反噬 / 夺舍·借尸 / 神念结界·锁魂困敌 / 分魂诱敌·金蝉脱壳 / 望气探魂
- **命修 ming_fate_causality**
  - 卜术：观星问卜 / 趋吉避凶诀 / 周天卦象演算 / 紫微斗数·命盘开演 / 太乙神数·一念演天
  - 推衍：青冥推演术 / 因果连珠算 / 万象归一推衍图 / 时空回溯·逆演前尘 / 推天衍命·大衍之纲
  - 夺运：借运术 / 截命符 / 移祸江东·因果转嫁 / 夺运改命大法 / 断人生死·绝命截运
  - 因果：因果落子 / 气运锁 / 天谴转移阵 / 逆天改命·承负护体 / 大衍五十·遁去其一
  - 战技：一念断因果 / 夺运截命·一击 / 逆演重开 / 移祸天下 / 趋吉闪命 / 借寿演天 / 断生死·绝命
- **丹修 dan_xiu**
  - 丹方：聚气丹方 / 回元疗伤丹方 / 破障筑基丹方 / 九转金身丹方 / 夺元化魂丹方
  - 火候：文火诀 / 武火真意 / 文武相济火候 / 心火不乱定鼎诀
  - 药理：百草辨识录 / 药力调和术 / 灵草催熟法 / 万古药藏通解
  - 控火：驭火基础心法 / 纳火入体诀 / 异火同源融炼 / 万火归炉帝焰诀
  - 战技：护身丹爆 / 毒烟丹·撒豆 / 回元急救 / 夺元一击 / 炸炉自爆 / 施丹结契 / 聚丹换酬
  - （丹药名生成式：`[效果意境]+丹`，意境词={聚气,回元,筑基,九转金身,夺元,定颜,青灵,三纹}）
- **鬼修 gui_xiu_yang_hun**
  - 鬼术：勾魂鬼音诀 / 七鬼噬魂术 / 玄魂炼妖大法 / 阴罗刹身决 / 幽冥血遁
  - 养魂：万鬼幡录 / 炼尸阴傀经 / 鬼婴夺胎法 / 阴兵借道符 / 镇魂锁鬼诀
  - 煞气：聚煞养元功 / 阴煞结丹诀 / 玄阴煞气罩 / 九幽煞河阵 / 夺舍重煞身
  - 战技：噬魂一缕 / 万鬼夜行 / 勾魂索命 / 阴风血雾 / 献祭弱鬼·镇魂 / 夺舍·借尸还魂 / 幽冥煞爆
- **佛修 buddhist_golden_body**
  - 佛法(般若)：般若波罗蜜心经 / 金刚般若功 / 大日如来真经 / 无量寿般若藏 / 诸天梵唱大光明经
  - 金身(炼体)：易筋经 / 金钟罩铁布衫 / 罗汉金身诀 / 金刚不坏神功 / 不动明王琉璃体
  - 禅定(愿心)：安那般那守息禅 / 四念处止观 / 九次第定 / 愿力回向法门 / 菩提无生大定
  - 愿行(降魔渡化)：护生戒杀行 / 伏魔金刚印 / 降魔杵法 / 渡化往生咒 / 我不入地狱大愿
  - 战技：狮子吼 / 韦驮献杵 / 摩诃无量佛光 / 金刚伏魔圈 / 大须弥山掌 / 渡魔往生印 / 不动明王怒目 / 诸天神佛降世
- **雷修 lei_xiu**
  - 雷法：五雷正法·掌心雷 / 惊蛰十二变 / 五府雷印 / 辟邪神雷 / 雷之古经·灭世雷光
  - 雷纹：地火社雷纹 / 避水御雷纹 / 纯阳灭阴纹 / 三十六雷·正雷纹 / 天劫本源纹
  - 承雷心法：引雷淬骨诀 / 雷火炼脉功 / 纯阳不坏体 / 渡劫吞雷功 / 肉身重铸·雷帝真身
  - 引雷身法：御风诀 / 风雷翅 / 五行遁·雷遁 / 缩地神雷步 / 雷光化身
  - 战技：引天劫 / 五雷轰顶 / 辟邪斩魂 / 雷狱困魔阵 / 舍身雷爆 / 惊雷破障 / 普化天雷·诛邪
- **驭兽师 yu_shou**
  - 驭兽术：百兽听令诀 / 伏魔降兽印 / 万兽朝宗箓 / 镇煞慑灵符 / 上古驭龙真解
  - 契约·御灵：血契缔结术 / 心魂同契法 / 九窍锁灵契 / 舍身护契大法 / 万灵归一御魂诀
  - 兽阵·役使：群狼撕咬阵 / 雁行围猎阵 / 蜂拥蔽天大阵 / 四象镇煞兽阵 / 百兽朝王绝阵
  - 灵兽心得·培育：喂养有方 / 灵料淬体 / 血脉返祖法 / 蛊虫孵生术 / 通灵化形诀
  - 战技：群兽突袭 / 灵兽附身·夺魄 / 嗜血催狂 / 断线应急·影替 / 万兽齐鸣·镇魂 / 灵兽献祭·爆体 / 召兽归阵

### 6.3 组合规则
```
// 主路径: 直出 canon 具名功法(占绝大多数, 保与已锁 ArtDef 同源)
artStream = NS_ART.Split(pathOrdinal(PathId))         // 每路独立子流, 顺序无关
配置 = SelectionRuleOf(PathId)                          // 各路深度设计的选取规则(每类目PickMin/Max)
foreach 类目 in path.ArtCategories:
    arts = pickN(类目.Arts直出清单, 类目.PickMin..PickMax, artStream, tier<=realmTierCap[realm])
战技 = pickN(path.CombatSkills, SkillPickMin..Max, artStream, tier门控)

// 回退路径(仅程序生成"新功法变体"时, 加内容用): 三段构词
新功法名 = pickWeighted(ArtTheme where PathPref==PathId)
         + (subRng<DoubleBodyChance ? pick(ArtBodyUse) : "")
         + pick(ArtBodyUse where 类别==类目)
         // 品阶/数目前缀按 art tier 门控(高阶字仅 tier>=4): 例 (三十六)+(雷)→"三十六雷", (神)+(剑法)+...
// 例(回退): 紫雷(theme法)+诀(术法后缀)→"紫雷诀"; 九(数目)+幽(theme鬼)+噬魂(核)+大法→"九幽噬魂大法"
```

### 6.4 境界称谓投影（RealmName，承 §3.2 + 各路 realmMultipliers 描述）
> 功法/角色显示常需「境界名」。境界名**不是本库自创**，直接投影 §3.2 力量层级总览表的「仙侠层称谓/武侠层称谓」双轨，按角色 `IdentityLayer`（武夫读武侠列、修士读仙侠列）+ 该路 `UnifiedTierOf[major]`：
```
仙侠(修士): 炼气→筑基→金丹→元婴→化神→炼虚→合体→大乘→(大乘巅)→[—]→渡劫→飞升→[域外/天人]
武侠(武夫): 不入流→二流→一流→后天巅峰→先天→宗师→大宗师→绝顶→陆地神仙下→陆地神仙→手中无招→[—]→[—]
// per-path 别名(取自各路曲线描述, 仅 flavor): 剑修UT顶="手中无剑/人剑合一/剑仙"; 体修="不灭金身/陆地神仙";
//   器修="人器合一/手中无器"; 魂修="阳神大成"; 雷修="手中有雷/雷帝真身"; 驭兽="身周无兽万兽可召"; 丹修="丹帝"
RealmName(角色) = IdentityLayer==Martial ? 武侠列[UTof(path,major)] : 仙侠列[UTof(path,major)]
```

---

## 7 · 地名词根（PlaceNamePool）—— 大区(州/域)→地标→Site 三级

> 承 §5.2 三级空间 + 命名大区固定骨架。构词：`地名 = [意象前缀] + [地形后缀]`，按空间层级选后缀子集。**固定骨架(大区/地标)用 canon 命名直出，随机微(秘境/资源点/坊市)才组合**。

### 7.1 意象前缀 `PlaceImagery`（与门派字根共享，保「门派坐落地名实相符」）
```
方位天象: 北 南 东 西 中 塞外 玄 紫 青 碧 金乌 凌霄
自然物: 落霞 通天 桃花 风雪 黄枫 掩月 寒潭 听涛 沧浪 云岫 万峰 栖云 流波 朱颜 大雷 灭云 销金
道佛词: 青云 焚香 真武 天音 莲花 净土 紫府 玄都
凶险/秘境(挂Peril/危险层): 百蛊 坠魔 葬天 黄泉 万蝠 滴血 九幽 阴煞 乱葬 古战
```

### 7.2 地形后缀 `PlaceTerrain`（**按空间层级约束**，承 §5.2）
```
大区(Region·州/域):   州 域 洲 陆 (例: 中州/东海域/苗疆)        // 6~12个, 固定
地标(Landmark·锚点):  山 谷 岭 海 泽 原 峡 关 渊(扣百蛊渊) 墟(扣剑墟) 京(神京) 漠(北漠)
Site(地点):           城 镇 堡 坊 阁 亭 峰 窟 洞 冢 岛 学宫 客栈 渡口 山门 秘境
秘境递进链(山→窟→洞, §考据): 山 → 古窟 → 滴血洞   // Site深度链, 秘境用
```

### 7.3 命名大区 canon 直出清单（固定骨架，§5.2 / §4.7）
```
中州(神京·大胤皇城所在) / 东海(剑墟·剑墟道盟) / 北漠(铁佛寺) / 西陲(万器谷·万器商会) /
南疆(百蛊渊·百蛊渊魔宗) / 苗疆(十万大山·古蛊一族) / 江南 …  (6~12, RegionDef.Name 固定)
```
- 大区**属性字段**（非命名，但命名受其约束）：`Element/TerrainClass/Peril` 决定该区 Site 名的意象偏好（南疆/苗疆偏「百蛊/瘴/蛊」阴诡词，北漠偏「漠/雪/铁」苍凉词）。

### 7.4 组合规则
```
// 大区/地标: 直出 GeoCanon 固定名(剑墟/百蛊渊/神京/铁佛寺...), 零随机, 跨纪元不变锚
// 随机微(秘境/资源点/坊市/散修聚落): 组合, 走 mapRng(Split7) 已锁流(命名子流NS_PLACE仅给"备选词", 实际roll在map流)
placeName = pick(PlaceImagery where 匹配所在Region的Element/Peril偏好)
          + pick(PlaceTerrain[空间层级])
// 例: 落霞(imagery)+峰(Site)="落霞峰"; 百蛊(imagery凶险)+渊(地标)="百蛊渊";
//     九幽+古窟→"九幽古窟"; 寒潭+洞→"寒潭洞"(秘境深度链)
// 全局唯一(地标): usedNames 过滤撞名; 秘境 ≤ MaxSecrets 上限内组合(§5.3)
```

---

## 8 · 戏剧/历史名号附池（DramaNamePool）—— 复仇弧/师承/历史锚点的 flavor

> 承 §7.4 HistoryAnchor 六类 + §戏剧（GrudgeLedger/storylet）。这些**只进 Chronicle PremiseTemplate**（零数值，§7.4）。构词复用上述池 + 专用事件词。

### 8.1 历史大劫/纪元名 `EpochName`（canon 固定，§2.1 / §7.2，直出不组合）
```
神魔纪(EraIndex0·上古) / 百圣纪(EraIndex1·乱古远古) / 江湖纪(EraIndex2·今世开局)
玄昊大劫(§2.1 道枢崩裂之劫·固定专名)  ;  年号: 承熹(ReignName·大胤朝, §2.1)
```

### 8.2 历史锚点名生成式 `AnchorName`（六类 × 命名成分，对齐考据「X劫/X战役/X诀/X禁地/X陵园/X洞府」）
```
Calamity(灭世大劫): [意象]+大劫/之劫     例: 玄昊大劫(固定) / 天倾之劫 / 神魔互戕
Battlefield(古战场): [意象]+古战场/战役/葬地  例: 葬天古战场 / 万骨原 / 上苍战役
LostLineage(失落传承): [path意境词]+[体用后缀]+残卷/遗篇  例: 大衍诀·残卷 / 青元剑诀·上界续篇
Feud(势力起源恩怨): [势力A]×[势力B]世仇   例: 剑墟道盟·百蛊渊·百年血仇(扣§4.7预置恩怨)
Bloodline(隐藏血脉): [复姓/古族]+血脉/遗脉  例: 轩辕血脉 / 古蛊一族·图腾遗脉
Fortune(气运分配): [势力]+国运/底蕴/气数    例: 大胤国运 / 剑墟气数
古迹/秘境/洞府(RelicSite): [意象凶险]+禁地/古窟/洞府/陵园  例: 百蛊禁地 / 滴血古窟 / 道枢碎片(扣§2.2)
```

### 8.3 复仇弧/师承 flavor `DramaTag`（GrudgeKind/师承谱系渲染词）
```
恩怨5态(§戏剧, 仅渲染): 结怨 / 寻仇 / 决裂 / 复仇 / 了结
师承词: 师尊 / 师叔 / 掌门真传 / 关门弟子 / 衣钵 / 道统
跨代恩怨(Grudge.Generation>0): 世仇 / 祖辈恩怨 / 血债 / 旧主裂解(扣考据"炼血堂衰败→四魔宗"范式)
```

---

## 9 · 落地集成、不变量、扩展验收（implementer 硬契约）

### 9.1 数据落地形态（全 L0 数据行）
```csharp
// 命名空间 Jianghu.Naming(新建, INV-FLOAT 覆盖: 零浮点——本库纯字符串+整数索引, 天然满足)
record NameRow(int RowId, string Token, int Weight,
               int AlignHint /*1/0/-1/无=99*/, byte FactionTypeHint /*255=无*/,
               string? PathPref /*pathId或null*/, byte ElementHint, byte Register, int RankBand);
record NamePool(string PoolKey, IReadOnlyList<NameRow> Rows);   // 加名=Rows.Add一行(L0)
// 全部池为静态数据表常量 NamingCanon(类比 GeoCanon), 生成期静态加载, 零运行期消费
record NamingCanon(NamePool Surname_Single, Surname_Compound, MaleGiven_Fierce, MaleGiven_Refined,
                   MaleGiven_Themed, FemaleGiven_Grace, FemaleGiven_Herb, DaoHao_Theme, DaoHao_Honorific,
                   FactionPrefix, FactionCore, /*FactionSuffix按Type分*/ Dictionary<FactionType,NamePool> FactionSuffix,
                   /*ArtNameBank按pathId分*/ Dictionary<string,ArtNameGroup> ArtNames,
                   ArtTheme, ArtBodyUse, ArtGrade, PlaceImagery, /*PlaceTerrain按层级*/ Dictionary<SpaceLevel,NamePool> PlaceTerrain,
                   EpochName, AnchorName, DramaTag);
```

### 9.2 组合器接口（纯函数、确定性、生成期一次性）
```csharp
static class NameForge {  // 全部方法: 输入(canon, 上下文标签, Pcg32 子流) → string, 纯整数索引, 可复现
  string Person(NamingCanon c, Sex sex, IdentityCtx ctx, Pcg32 rng);   // 姓+名
  string DaoHao(NamingCanon c, int unifiedTier, string? pathId, int align, Pcg32 rng);
  string Faction(NamingCanon c, FactionType t, int align, int? landmarkCoreRef, Pcg32 rng);
  ArtLoadout Arts(NamingCanon c, string pathId, int realm, Pcg32 rng);  // 直出 canon 具名功法配置
  string Place(NamingCanon c, SpaceLevel lvl, RegionCtx region, Pcg32 rng);
  string RealmName(string pathId, int major, IdentityLayer layer);     // 投影§3.2双轨, 无rng
}
// 接入点: WorldFactory.CreateInitial 内, faction/spawn/geo/history Seed 阶段调用,
//   rng = genRng.Split(GEN_NAMING=102).Split(NS_*).Split(entityOrdinal)
```

### 9.3 不变量（gate）
- **INV-NAME-DET**：同 seed + 同上下文标签 → 同名号，逐字节一致（命名走 Split 子流，顺序无关）。
- **INV-NAME-OFF**：naming/faction/geo/cult off → 不构造 NamingCanon、不调 `Split(102)`，既有 `Split(1..4)` 派生序不变 → INV-OFF-38 不破。
- **INV-NAME-NOLEAK**：名号零进 StatBlock/Σ=80/EffectivePower/TribScore；只写 Name/Origin/Chronicle flavor 字段（可证伪：固定双方机制输入、仅换名号种子，所有数值逐字节不变）。
- **INV-NAME-CANON**：12 路功法名直出清单 ⊆ 深度设计已锁 ArtDef/CombatSkillDef 名集（生成的功法名不得脱离已锁内容库杜撰）；六大势力名/命名大区名 = §4.7/§5.2 canon 固定名。
- **INV-NAME-BOUNDED**：全局唯一名撞名重抽 ≤ `MaxNameRetry`，超限走 `基名+序号` 兜底；稀有成分（复姓/天尊级尊号/至宝级器名）受 RareWeight + RankBand 门控，密度受控（随机有上限）。

### 9.4 不锁死验收（必含异构 case，承 §9 L0 纪律）
- **加 1 个名** = 对应 `NamePool.Rows` 追加一行 → 立即可被抽到，零改组合器（L0）。
- **加 1 路功法名** = `ArtNames` 加一个 `pathId` 分组（若 World Bible 扩到第 13 路）→ `Arts()` 自动覆盖，零改 NameForge（L0）。
- **加 1 类成分轴**（如「绰号池」「兵器名池」）= 加一张 NamePool + 加一个 `NS_*` 子流常量 + 加一个 NameForge 方法 → 不动既有池（L0 数据 + 1 新方法，引擎读表无特例）。
- **加 1 个 FactionType 的后缀子集** = `FactionSuffix` 字典加一个 key（随 §4.2 加 FactionType 枚举值时同步，L1 枚举 + L0 表行，诚实标注）。

---

## 附：本库与 canon 对齐自检（无冲突核对）
- **12 路**：功法名严格按 `pathId` 分组直出已锁具名功法；境界名投影 §3.2 双轨，不自创境界体系。
- **三轴境界**：RealmName 读 `UnifiedTierOf[major]` + IdentityLayer，不碰 flatIndex/(MajorTier,SubLevel) 编码。
- **8 Faction**：门派名后缀按 `FactionType` 子集约束 + `AlignmentAxis` 偏好；六大初始势力 canon 名直出。
- **RngStreamIds**：命名走 `genRng=Split(1)` 命名子流 `GEN_NAMING=102`（类比 GEN_HISTORY=101），生成期一次性消费，不进 Clone（运行期不消费），不开新顶层号。
- **侧表纪律**：名号只写既有 flavor 字段（FactionDef.Name/Persona.Origin/ArtDef.Name/LandmarkDef.Name），核心 record 字段顺序一字不改。
- **软克制**：本库零涉战斗克制（命名不进任何 path-vs-path 逻辑），与软情境哲学不冲突。
- **红线①可程序化**：纯整数索引 + 加权轮盘 + 三类上限（频率/强度/并发）；红线②不锁死：加名=加 L0 数据行。
- **归属九野**：全部成分扣「江湖纪/大胤·承熹/镇玄司/道枢/玄昊大劫/末劫将临/剑墟·百蛊渊·铁佛寺·万器谷·苗疆古蛊」等 canon 专名，无通用化。

---

# 第六部 · 12 路功法补遗库

# 12 路功法补遗库 · 九野 · 江湖纪（承熹年）

> 定位：本库**只补遗、不重造**。已读 PERPATH_DOC（12 路逐路：特色机制/terms/realmMul/修炼途径/功法类目 3~4 类各 4~5 具名 art/战技 7 条/选取规则）与 A3-FINAL §3（12 路 daoHeart 机制表：innerDemon源/daoHeart增益源/主劫 ResistTerms/突破货币/特色失败/daoHeart_init/Purge 通道）。两份已覆盖：①各路 art 类目≥3、每类≥4 具名 art、战技≥5（已满足，不复制）；②daoHeart 的**机制**（资源、权重、阈值）。
>
> 本库唯一新增的「具名内容」层 = A3 表与 PERPATH 都缺的**命名实体**：
> 1. **每路 1 个新增「道心类目」**（ArtCategoryDef，与既有 3~4 类**并列追加**，role=`daoheart`，PickMin/Max=1），内含 ≥4 具名「道心功法/心诀」——其 EffectOp 只动 `CultivationState.Resources["daoHeart"/"innerDemon"/"comprehension"]` 与 TribScore/Phase，**严禁进 EffectivePower**（INV-DECOUPLE：corr(daoHeart,Insight)<0.7）。
> 2. **每路具名「道心境界阶」**（flavor + 整数阈值，锚定 `T_DAO_FIRM=60`/`COMP_CAP=100`）。
> 3. **每路具名「心魔」**（innerDemon≥80 走火态的命名 flavor；魂修≥95），接 §6.5「引动心魔」第二扇门。
> 4. **每路 daoHeart 类「顿悟 storylet / 道心战技」补遗**（≥1，接既有 source key：epiphany/askSwordMaster/refineSoul/chanding/forgePerfect…）。
> 5. 个别路**补 1~2 具名 art / 战技**（仅当能丰富 L0 行且不与既有重名）。
>
> **红线遵守**：纯数据行 L0（加路加内容=追加行，零改引擎）；整数+随机有上限；严格归属九野（道枢/末劫/大胤镇玄司/江湖纪/承熹），不通用化；与已锁系统（12 路/三轴境界 UT0-12/8 Faction/RngStreamIds Split5=cult/侧表纪律/软克制 ±P0/4）对齐。

---

## 0. 通用 schema（道心层数据行格式，复用 A 的 record）

道心功法 = `ArtDef` 的特例（挂在新增 role=`daoheart` 的 `ArtCategoryDef` 下），**Power=0**（不进战力），效果只走以下 EffectOpKind 子集（全部已在 A schema 枚举内，零新算子）：

```
道心功法行 = { artId, name, tier, power:0,
  effects:[ EffectOp ],          // 仅用 AddResource/AddResourceCap/ScalarMul/SetFlag/AdjustRelationEdge/GrantPassive
  gates:{ minRealm?, minDaoHeart?, maxInnerDemon? } }

允许的 EffectOp.TargetKey（道心层私有，经 ApplyResource chokepoint 钳制）:
  "daoHeart"[0,100]   "innerDemon"[0,100]   "comprehension"[0,COMP_CAP=100]
  "tribResist"(只在突破劫 ResistTerms 注入,不进 EffectivePower)
  flag: "daoFirm"(daoHeart≥T_DAO_FIRM=60 派生,只读)  "deviating"(走火态,innerDemon≥80)
```

道心境界阶行 = `{ pathId, stageKey, name, daoHeartAtLeast, comprehensionAtLeast?, note }`（纯 flavor + 整数门，仅入 Chronicle/显示，零数值路径进战力）。

心魔行 = `{ pathId, demonKey, name, triggerInnerDemon(默认80,魂修95), deviationFlavor, deviationDebuffNote(经 §6.5 DeviationDebuff flag,打折战力不扣四维) }`。

> 三轴正交铁律（承 A3 §3.2/§6.5）：`daoHeart`(稳道)↑ ⊥ 战力资源轴；`innerDemon`(心魔)↑→ 心魔劫 ResistTerms 负项 + 走火门；`comprehension`(顿悟积累)→ breakProgress/小境界。**全 12 路共用此三键，差异只在「增益源/污染源/命名」**。

---

## 1. 剑修 sword_immortal — 道心：无漏剑心

**新增道心类目** `swordheart`（剑心·斩道之念｜PickMin/Max=1；与既有 剑法/心法/身法/剑意 四类并列，第 5 类）

| artId | name | tier | EffectOp 摘要（effects；power=0） | gates |
|---|---|---|---|---|
| dh_sw_chijian | 持剑问心诀 | t1 | AddResource(daoHeart,+4)；每场切磋后 AddResource(comprehension,+1) | minRealm:0 |
| dh_sw_shouzhuo | 守拙剑心录 | t2 | AddResource(innerDemon,−2,下限0)；SetFlag 守拙：剑意空(swordWill=0)入战时 innerDemon 增速减半 | minRealm:1 |
| dh_sw_wenjian | 问剑名宿心得 | t3 | AddResource(daoHeart,+6)（接 source `askSwordMaster+6`）；问剑后 zhanDaoMiles 之外另 comprehension+2 | minRealm:2 |
| dh_sw_wulou | 无漏剑心经 | t4 | AddResourceCap(daoHeart,+0 但锁下限 daoHeart≥30)；化神后心魔劫 ResistTerms 注 (daoHeart,+4) | minRealm:4,minDaoHeart:40 |
| dh_sw_renjian | 人剑两忘·太上忘情心 | t5 | GrantPassive：daoHeart≥T_DAO_FIRM 时「剑意不散」保留上限+5；innerDemon≥80 时本心法暂失效（忘情反成执） | minRealm:6,本命剑开光 jianCheng=100 |

**道心境界阶**（daoHeart 门）：持剑(≥0「初执一剑，意气未平」)→守拙(≥30「藏锋于钝，不逞剑意」)→剑心通明(≥60「daoFirm，临强不乱」)→无漏(≥85「斩道见血而心不染杀」)。

**心魔**：`demon_sword_pianfeng`「剑走偏锋」——triggerInnerDemon=80；剑意空+ID≥80 触（承 A3 `swordWillEmptyInCombat`）；deviationFlavor=「执念入魔，逢人即斩、见强必拔，斩道沦为杀道」；DeviationDebuff：本场出剑命中判定 −，对无辜目标 AdjustRelationEdge(self←场上正派, −) 自动结死仇（接 innerDemon 源 `killWithoutCause+4`）。

**顿悟战技补遗**：`dh_sw_jianfen_epiphany`「剑冢悟剑·顿悟」[t3]——入「剑冢」storylet 触发；EpiphanyRoll<Insight−18 则 comprehension+25 或 daoHeart+5（cultRng.Split(5) 抽多结局）；冷却 EPIPHANY_COOLDOWN=50；接既有 daoHeart 源 `epiphany+8`。

---

## 2. 体修 ti_xiu_hengshi — 道心：金石不移心

**新增道心类目** `bodyheart`（武胆·横世之志｜PickMin/Max=1；第 4 类）

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_ti_zhagen | 扎根桩心诀 | t1 | AddResource(daoHeart,+3)；闭关枯坐 streak 时 innerDemon 增速 −1（缓 A3 SECLUDE_DEMON） | minRealm:0 |
| dh_ti_aida | 挨打养性功 | t2 | 每被穿透 1 次 AddResource(daoHeart,+1,本场上限+6)（接 source `enduranceVictory+5` 同源「以耐久立心」） | minRealm:1 |
| dh_ti_jinshi | 金石不移心经 | t3 | AddResource(innerDemon,−3)；天劫败损 qixue 时 daoHeart 不跌（兜「跌境惩罚轻」叙事） | minRealm:3 |
| dh_ti_wugu | 武骨铮铮志 | t4 | 天劫 ResistTerms 注 (daoHeart,+3)（呼应 A3 体修主劫 [Con×4,henglian×3,qixue×1,EP×1] 之外的心性兜底）；GrantPassive daoFirm 时 Con 突破判定 +1 档 | minRealm:5,minDaoHeart:40 |

**道心境界阶**：扎根(≥0「站如桩，挨打不退」)→坚毅(≥30)→金石不移(≥60「daoFirm，泰山崩于前色不变」)→不灭武心(≥85「肉身可碎，武志不堕」)。

**心魔**：`demon_ti_xuewang`「血气妄动」——triggerInnerDemon=80；deviationFlavor=「久挨不还，戾气积胸，燃血不止、罔顾自身，把横练沦为自残搏命」；DeviationDebuff：本场不灭金身复活后 henglian 不回满、自伤项翻倍打折（不扣四维，经 DeviationDebuff flag）。

**补遗战技**：`dh_ti_zuowang`「枯坐参道」[t1]——闭关 DailyMode=Steady 强化：innerDemon−1 之外 AddResource(daoHeart,+2)、Foundation+1（接 A3 daoHeart 源 `meditate+2`）。

---

## 3. 法修 fa_xiu — 道心：明镜止水心

**新增道心类目** `spellheart`（法心·参法之念｜PickMin/Max=1；第 4 类）

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_fa_jingshui | 明镜止水诀 | t1 | AddResource(daoHeart,+3)；manaPool 耗尽连败 streak 时 innerDemon 增速 −1（缓 A3 `manaExhaustedDefeat+2`） | minRealm:0 |
| dh_fa_canfa | 参法悟道心 | t2 | 法术参悟成功 AddResource(comprehension,+2)（接 source `spellComprehend+5`）；每多修一系 spellBreadth 时 daoHeart+1 一次性 | minRealm:1 |
| dh_fa_buzheng | 不诤元神录 | t3 | AddResource(innerDemon,−3)；被元素相克吃亏(elementCounterLoss)时 daoHeart 不跌 | minRealm:3 |
| dh_fa_wuxiang | 无相清静经 | t4 | 渡劫 ResistTerms 注 (daoHeart,+3)；GrantPassive daoFirm 时续航惩罚阈值再放宽（叙事「心定则法不乱」，不改战力数值结构，仅调 flag 门） | minRealm:5,minDaoHeart:40 |

**道心境界阶**：引气(≥0)→明镜(≥30「照而不染」)→止水(≥60「daoFirm，万法过心不留痕」)→清静无相(≥85)。

**心魔**：`demon_fa_zoukou`「灵根走窜」——triggerInnerDemon=80；deviationFlavor=「贪修多系、强催未稳之法，主灵根与杂修相冲，法力倒灌经脉」；DeviationDebuff：本场被克 counterAdj 负值加深（仍受 ±P0/4 clamp）、续航惩罚提前触发。

**顿悟补遗**：`dh_fa_dongxuan`「参《诛仙天书·正卷》顿悟」[t4]storylet——同节点有高阶法修「论道」被动触发（承 A3 §4.3 论道入口）；comprehension+25 或 spellBreadth 突破封顶进度+1（cultRng.Split5 抽）。

---

## 4. 阵修 array_formation — 道心：定盘星心

**新增道心类目** `arrayheart`（阵心·算阵之念｜PickMin/Max=1；第 4 类）

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_ar_dingpan | 定盘星诀 | t1 | AddResource(daoHeart,+3)；未布阵被偷家(ambushedUnarrayed)后 innerDemon 增速 −1 | minRealm:0 |
| dh_ar_tuiyan | 推演静心录 | t2 | 演阵成功 AddResource(comprehension,+2)；compute 满载推演时 daoHeart+1 | minRealm:1 |
| dh_ar_buluan | 临阵不乱心经 | t3 | AddResource(innerDemon,−3)；标志阵布成(arrayConsummate)时 daoHeart+6（接 source `arrayConsummate+6`） | minRealm:2 |
| dh_ar_tianyuan | 天元归一阵心 | t4 | 渡劫 ResistTerms 注 (daoHeart,+3)（呼应 A3 阵修主劫含 arrayed_flag/terrain 门）；GrantPassive daoFirm 时被「破阵眼」打断需额外 +1 命中 | minRealm:5,minDaoHeart:40 |

**道心境界阶**：识阵(≥0)→定盘(≥30「乱中有数」)→临阵不乱(≥60「daoFirm，偷家不慌、急布不错」)→天元归一(≥85「胸有全局，阵随心转」)。

**心魔**：`demon_ar_zhangcha`「掌算入魔」——triggerInnerDemon=80；deviationFlavor=「算尽天机反被天机所执，疑神疑鬼、阵阵设杀，于荒地亦强布必败之阵」；DeviationDebuff：本场未布阵裸值 floor 不抬升、setup 被打断概率上升（不扣四维）。

**补遗战技**：`dh_ar_wuxiangxinzhen`「无相心阵·静念」[t5]——非伤害；本场 AddResource(innerDemon,−4)、daoHeart+5，缓解阵修「孤注一掷算计」积心魔；接 A3 Purge 通道 `meditate`。

---

## 5. 器修 qixiu_artificer — 道心：器我两忘心

**新增道心类目** `forgeheart`（器心·人器相照之念｜PickMin/Max=1；第 5 类，与 炼器术/器纹学/御宝心法/藏宝阁 并列）

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_qi_xinbao | 心宝相照诀（道心篇） | t1 | AddResource(daoHeart,+3)；soulBond 每满一格 daoHeart+1（人器相亲养心） | minRealm:0 |
| dh_qi_chenglu | 成炉静定录 | t2 | 祭炼/炸炉后 AddResource(comprehension,+2)；itemTier 跟不上境界(itemLagBottleneck)时 innerDemon 增速 −1 | minRealm:1 |
| dh_qi_buduo | 宝失不夺心经 | t3 | AddResource(innerDemon,−3)；本命法宝被落宝(artifactSnatched)后 daoHeart 不跌（缓「脱宝即崩」心态雪崩） | minRealm:3 |
| dh_qi_qiwo | 器我两忘·人器合一心 | t4 | 渡劫 ResistTerms 注 (daoHeart,+3)；GrantPassive daoFirm 时器灵影子 itemTier 加成判定更稳（仅 flag 门，不改 +40 数值） | minRealm:5,minDaoHeart:40,本命法宝 itemTier≥6 |

**道心境界阶**：认主(≥0)→相照(≥30)→宝失不乱(≥60「daoFirm，夺我宝而道心不堕」)→器我两忘(≥85「手中无器，本命不夺」)。

**心魔**：`demon_qi_lianbao`「炼宝着相」——triggerInnerDemon=80；deviationFlavor=「贪宝成癖，为求高阶强行越境祭炼、夺人本命，视宝重于命」；DeviationDebuff：本场 soulBond 榨取打折、被「御纹」反夺更易（不扣四维）。

**顿悟补遗**：`dh_qi_qiling`「器灵开窍·顿悟」[t4]storylet——soulBond=20 且 itemTier≥6 时触发（接 source `spiritAwaken+8`）；daoHeart+5 或 comprehension+25。

---

## 6. 魂修 soul_divine_sense — 道心：识海澄明心（daoHeart_init ×3 最高）

**新增道心类目** `soulheart`（神心·养识之念｜PickMin/Max=1；第 4 类，与 神识/魂术/秘术 并列）

> 魂修为 12 路心魔最烈者（innerDemon 走火阈 **95**，反震 reverbBacklash+5 本路最高），故道心层 art 偏「兜底自救」。

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_so_shouyi | 守一养神心诀 | t1 | AddResource(daoHeart,+4)；SeaIntegrity 上限锁定不低于 20（呼应既有 守一养神术，但此为道心轴效果） | minRealm:0 |
| dh_so_chengming | 识海澄明录 | t2 | 炼神回升时 AddResource(daoHeart,+4)（接 source `refineSoul+4`）；神识探查成功 daoHeart+3（接 `divineProbeSuccess+3`） | minRealm:1 |
| dh_so_buzhui | 神魂不坠心经 | t3 | AddResource(innerDemon,−4)；反震致 SeaIntegrity 暴跌后 daoHeart 不跌 | minRealm:2 |
| dh_so_yangshen | 阳神不灭道心 | t4 | 心魔劫 ResistTerms 注 (daoHeart,+4)（A3 魂修主劫 [DH×4,seaIntegrity×3,Ins×2,ID×−4]，本心法坐实 DH×4 来源）；GrantPassive daoFirm 时夺舍失败的 SeaIntegrity 扣减减半 | minRealm:4,minDaoHeart:40 |

**道心境界阶**：凝魂(≥0)→澄明(≥30)→神魂不坠(≥60「daoFirm，反震不溃」)→阳神不灭(≥85「神念所至，心如止水」)。

**心魔**：`demon_so_shihaibengl`「识海崩裂·魔念噬主」——triggerInnerDemon=**95**（本路特高，承 A3 `ID≥95识海崩裂`）；deviationFlavor=「夺舍成瘾、噬魂无度，分魂尽灭后本魂为魔念所占，神智迷狂」；DeviationDebuff：本场 SpiritPen 击穿失败反震全额回灌、SeaIntegrity 乘子门骤降（不扣四维）；自救=`refineSoul`（SeaIntegrity 低则失效，承 A3 Purge 通道）。

**补遗秘术**：`dh_so_guiyuan_shoushi`（道心兜底）「归元守识·钳崩」[t3]——已在既有秘术池，此补其道心效果：触发钳 SeaIntegrity=10 保命的同时 AddResource(daoHeart,+3)、innerDemon−5，给低容错路一次「死里悟道」。

---

## 7. 命修 ming_fate_causality — 道心：知命不惧心

**新增道心类目** `fateheart`（命心·承负之念｜PickMin/Max=1；第 5 类，与 卜术/推衍/夺运/因果 并列）

> 命修以 Karma/LifespanDebt 为货币，innerDemon 源 `karmaOverflow(每点差+1)`，故道心层主「清债定心、抗反噬雪崩」。

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_mi_zhiming | 知命安神诀 | t1 | AddResource(daoHeart,+3)；卜中应验(divineSuccess)时 daoHeart+1（接 source `divineSuccess+4`） | minRealm:0 |
| dh_mi_qingye | 清业定心录 | t2 | Karma 被清(karmaCleared)时 AddResource(daoHeart,+5)（接 source `karmaCleared+5`）；因果落子兑现时 innerDemon−1 | minRealm:1 |
| dh_mi_buju | 知命不惧心经 | t3 | AddResource(innerDemon,−3)；撞「大气运反弹」(backlashFromGreaterFate)后 daoHeart 不跌 | minRealm:3 |
| dh_mi_woming | 我命由我承负心 | t4 | 渡劫 ResistTerms 注 (daoHeart,+3)；GrantPassive daoFirm 时净气运被打穿(NetFortune≤0)濒死自救判定更稳（仅 flag，不改「大衍五十·遁去其一」数值） | minRealm:5,minDaoHeart:40 |

**道心境界阶**：观星(≥0)→知命(≥30)→知命不惧(≥60「daoFirm，算无可算亦不慌」)→我命由我(≥85「逆天改命而心不堕」)。

**心魔**：`demon_mi_yexin`「业火攻心」——triggerInnerDemon=80；deviationFlavor=「透支夺运、强夺命大者，天谴 Karma 雪崩反噬，疑天怨命、夺运成瘾」；DeviationDebuff：本场 NetFortune 计算时 Karma 权重加重（更易转负→反噬）、Backlash reflect 不被「承负护体」抵消（不扣四维）。

**补遗战技**：`dh_mi_yinguoluozi_xin`「因果落子·定心」[t1]——既有「因果落子」补道心效果：预埋因果子后 AddResource(daoHeart,+2)、innerDemon−1（接 A3 Purge 通道「meditate+因果落子(清Karma降ID)」）。

---

## 8. 丹修 dan_xiu — 道心：丹火定鼎心

**新增道心类目** `pillheart`（丹心·守炉之念｜PickMin/Max=1；第 5 类，与 丹方/火候/药理/控火 并列）

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_da_shoulu | 守炉静心诀 | t1 | AddResource(daoHeart,+3)；炸炉(furnaceExplode)后 innerDemon 增速 −1 | minRealm:0 |
| dh_da_danwen | 丹纹悟道录 | t2 | 炼成满纹丹(pillPerfect)时 AddResource(comprehension,+2) + daoHeart+1（接 source `pillPerfect+5`） | minRealm:1 |
| dh_da_buji | 临炉不急心经 | t3 | AddResource(innerDemon,−3)；火候失稳时不暴走（叙事，定 flag「定鼎」降炸炉心理崩） | minRealm:2 |
| dh_da_dinging | 文武定鼎道心 | t4 | 渡劫 ResistTerms 注 (daoHeart,+3)；GrantPassive daoFirm 时灵魂感知(悟性)卡丹阶上限判定更稳 | minRealm:5,minDaoHeart:40 |

**道心境界阶**：起炉(≥0)→守炉(≥30)→临炉不急(≥60「daoFirm，炸炉不馁、强敌临门仍稳炼」)→丹火定鼎(≥85「炉火纯青，丹我两忘」)。

**心魔**：`demon_da_huoxin`「火噬丹心」——triggerInnerDemon=80；deviationFlavor=「贪炼帝品、强引异火越境，火毒攻心，丹成而人疯，毒丹滥施害人」；DeviationDebuff：本场 directPower 受击折扣更重（直面更易被秒）、毒丹暗杀分支暴露率升（不扣四维）。

**补遗战技**：`dh_da_dingxindan`（自救型）「自炼定心丹」[t2]——非战斗主动；消成品丹库存，AddResource(innerDemon,−5)、daoHeart+3（接 A3 Purge 通道「meditate+自炼定心丹(资源型)」，把丹修资源优势转为道心自救）。

---

## 9. 鬼修 gui_xiu_yang_hun — 道心：守魂不噬心（daoHeart 低且易污）

**新增道心类目** `ghostheart`（鬼心·镇魂之念｜PickMin/Max=1；第 4 类，与 鬼术/养魂/煞气 并列）

> A3 标鬼修 daoHeart_init ×2「低且易污」，且**强制选 1 个 brake family（镇魂/献祭）**。本类目为道心轴的「刹车」补强，与战技层 brake 互补。

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_gu_shouhun | 守魂镇心诀 | t1 | AddResource(daoHeart,+3)；镇魂(zhenhun)成功时 daoHeart+1（接 source `zhenhun+4`） | minRealm:0 |
| dh_gu_jisha | 祭弱护本录 | t2 | 献祭弱鬼(sacrificeGhost)时 AddResource(daoHeart,+2)（接 source `sacrificeGhost+2`）；devourMeter 跨阈前 innerDemon 增速 −1 | minRealm:1 |
| dh_gu_busha | 噬而不堕心经 | t3 | AddResource(innerDemon,−4)（鬼修最需的降魔项）；噬魂(soulDevour)后阴德污点的 daoHeart 跌幅减半 | minRealm:2 |
| dh_gu_cunshen | 存神不化道心 | t4 | 心魔劫 ResistTerms 注 (daoHeart,+4)（A3 鬼修主劫 [DH×4,Ins×2,ID×−4,shaCharge×1] 坐实 DH×4）；GrantPassive daoFirm 时 devourMeter 反噬阈值上调 | minRealm:4,minDaoHeart:40 |

**道心境界阶**：游魂(≥0)→守魂(≥30)→噬而不堕(≥60「daoFirm，噬魂而神智不失」)→存神不化(≥85「阴神凝实，魔念不侵」)。

**心魔**：`demon_gu_shizhu`「噬主反吞·魔化」——triggerInnerDemon=80；deviationFlavor=「养鬼无度、噬魂成瘾，devourMeter 暴走，所养鬼婴反噬夺舍，人沦为鬼、煞气暴走自伤」；DeviationDebuff：本场鬼兵 bond 计入比例骤降、煞气护体对纯阳/佛门/雷法减免失效加倍（不扣四维）；与既有「招雷劫/纯阳劫(纯阳tag→负adj)」叠合，坐实鬼修天克。

**补遗战技**：`dh_gu_zhenhun_dingxin`「镇魂锁鬼·定心」[t1]——既有「镇魂锁鬼诀」补道心效果：压 devourMeter 同时 AddResource(daoHeart,+2)、innerDemon−2（强制 brake family 的道心兜底）。

---

## 10. 佛修 buddhist_golden_body — 道心：菩提无生心（daoHeart_init ×3 最高，佛心即道心）

**新增道心类目** `chanheart`（禅心·愿心之念｜PickMin/Max=1；第 5 类，与 佛法/金身/禅定/愿行 并列）

> A3：佛修 daoHeart_init ×3「佛心即道心」、Purge=`chanding`（菩提无生近必过）。本类目把「愿/功德→道心」的命名层补全（与既有 禅定 类目侧重升境效率不同，本类目侧重 daoHeart/innerDemon 直接清算）。

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_fo_anxin | 安心守息禅心诀 | t1 | AddResource(daoHeart,+1+Insight/8)（接 source `chanding+1+Ins/8`）；禅定一次 daoHeart+1 | minRealm:0 |
| dh_fo_hush | 护生养愿录 | t2 | 护生/救人(saveLife)时 AddResource(daoHeart,+6)（接 source `saveLife+6`，纯事件驱动非 Insight 派生，助 INV-DECOUPLE） | minRealm:1 |
| dh_fo_buchen | 不嗔不痴心经 | t3 | AddResource(innerDemon,−4)；目睹恶行未渡(witnessEvilUnsaved)的 innerDemon+ 减半 | minRealm:2 |
| dh_fo_puti | 菩提无生大定心 | t4 | 心魔劫 ResistTerms 注 (daoHeart,+4)（A3 佛修主劫 [DH×4,merit×2/100,Ins×2,ID×−4]，几乎必过）；破戒(vowBroken)后 daoHeart 跌幅减半（缓「vow×1/2+ID+40」冲击） | minRealm:4,minDaoHeart:40 |

**道心境界阶**：发愿(≥0)→护生(≥30)→不嗔不痴(≥60「daoFirm，临邪不动、受围攻而愿心更盛」)→菩提无生(≥85「我不入地狱，谁入地狱」)。

**心魔**：`demon_fo_poji`「破戒入魔·嗔火」——triggerInnerDemon=80；deviationFlavor=「嗔怒杀戮、以杀代渡，愿力清零、功德冻结，佛光转为业火，金身染血」；DeviationDebuff：本场 anti_evil 倍率失效（对阴邪不再放大）、佛光招式只算 base 半数（不扣四维）；自救=清愿（愿力回向法门 / 菩提无生大定，ID−X，承 A3 Purge 通道）。

**补遗战技**：`dh_fo_huixiang`「愿力回向·清心」[t4]——既有「愿力回向法门」补道心效果：把愿力按 2:1 转功德的同时 AddResource(innerDemon,−6)、daoHeart+4，破戒后的核心自救。

---

## 11. 雷修 lei_xiu — 道心：纯阳无垢心

**新增道心类目** `thunderheart`（雷心·承雷之念｜PickMin/Max=1；第 5 类，与 雷法/雷纹/承雷心法/引雷身法 并列）

> A3：雷修「陨落: Con<thr 雷噬致残，**与心魔劫无关**」；故雷修道心层不主导生死劫，而主「承雷不馁、破邪不堕」与顿悟御雷。

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_le_chengshen | 承雷养性诀 | t1 | AddResource(daoHeart,+3)；雷噬致残(crippledByThunder)后 innerDemon 增速 −1 | minRealm:0 |
| dh_le_chunyang | 纯阳无垢心录 | t2 | 以雷淬体(refineByThunder)时 AddResource(daoHeart,+5)（接 source `refineByThunder+5`）；身处煞气/阴邪场域 daoHeart 不跌 | minRealm:1 |
| dh_le_buwei | 承雷不馁心经 | t3 | AddResource(innerDemon,−3)；引天劫渡劫失败损伤后 daoHeart 不跌 | minRealm:3 |
| dh_le_leidi | 雷帝真身道心 | t4 | 天劫 ResistTerms 注 (daoHeart,+3)（雷修主劫 [Con×4,thunderCharge×3,EP×2,leiwen×1] 之外心性兜底）；GrantPassive daoFirm 时承雷阈值 thr 容错等效 −1（叙事「心定则雷不噬」，与「引雷淬骨诀」叠加但仅调阈值容错 flag） | minRealm:5,minDaoHeart:40 |

**道心境界阶**：引雷(≥0)→纯阳(≥30)→承雷不馁(≥60「daoFirm，渡劫不躲、破邪不退」)→雷帝真身(≥85「以身为雷，垢念俱焚」)。

**心魔**：`demon_le_leikuang`「雷火焚心·暴走」——triggerInnerDemon=80；deviationFlavor=「嗜杀破邪、滥引天劫，雷火攻心，逢阴邪即不计自损强轰，致残亦不止，戾气如雷」；DeviationDebuff：本场「舍身雷爆」类自损项强制触发更频、对纯阳/正道目标的自损雷力加倍（不扣四维）。

**顿悟补遗**：`dh_le_yulei`「御雷顿悟」[t3]storylet——承雷/避劫时触发（接 source `epiphany+6`）；comprehension+25 或解锁「三十六雷·正雷」领悟进度+1（cultRng.Split5 抽）。

---

## 12. 驭兽 yu_shou — 道心：人兽同心

**新增道心类目** `beastheart`（驭心·同契之念｜PickMin/Max=1；第 5 类，与 驭兽术/契约御灵/兽阵役使/灵兽心得培育 并列）

| artId | name | tier | EffectOp 摘要 | gates |
|---|---|---|---|---|
| dh_yu_tongqi | 同契安神诀 | t1 | AddResource(daoHeart,+3)；养护主兽(raiseGuardBeast)时 daoHeart+1（接 source `raiseGuardBeast+4`） | minRealm:0 |
| dh_yu_qunxin | 群灵归心录 | t2 | 兽阵布成(beastFormationConsummate)时 AddResource(daoHeart,+5)（接 source `beastFormationConsummate+5`）；被音修乱兽后 innerDemon 增速 −1 | minRealm:1 |
| dh_yu_buqi | 失兽不弃心经 | t3 | AddResource(innerDemon,−3)；灵兽阵亡/被夺(beastDevourGrudge)后 daoHeart 不跌 | minRealm:2 |
| dh_yu_renshou | 人兽合一道心 | t4 | 渡劫 ResistTerms 注 (daoHeart,+3)（A3 驭兽主劫 rosterWeighted×3 之外心性兜底）；GrantPassive daoFirm 时本体被斩首的「断线」缓冲判定更稳（仅 flag，不改「舍身护契/影替」数值） | minRealm:5,minDaoHeart:40 |

**道心境界阶**：缔契(≥0)→同契(≥30)→失兽不弃(≥60「daoFirm，断线不慌、爱兽如己」)→人兽合一(≥85「身周无兽而万兽可召，心即阵眼」)。

**心魔**：`demon_yu_xuesi`「血祭噬心·驭兽成魔」——triggerInnerDemon=80；deviationFlavor=「为强兽滥施血祭、嗜杀催狂，bond 网震荡反噬本体，视灵兽为耗材、视生灵为饲料」；DeviationDebuff：本场 bond 衰减加速、灵兽献祭/嗜血催狂的反噬 −bond 加倍（不扣四维）；与「本体斩首→全栏脱契」脆性叠合放大。

**补遗战技**：`dh_yu_wanshou_zhenhun`「万兽齐鸣·镇魂」[t3]——既有「万兽齐鸣·镇魂」补道心效果：反·乱兽、全体 bond+10 的同时 AddResource(self.innerDemon,−4)、daoHeart+3（接 A3 Purge 通道「meditate+万兽齐鸣镇魂」）。

---

## 13. 落地汇总（交 implementer/auditor）

**新增数据行清单（全 L0，零改引擎）**：
- 12 个新 `ArtCategoryDef`（role=`daoheart`，每路 1 个，与既有 3~5 类并列追加，PickMin/Max=1）。
- 48 条新道心 `ArtDef`（每路 4 条，power=0，effects 仅用既有 EffectOpKind 子集）。
- 12 条新「顿悟/道心战技」`CombatSkillDef`（family 建议归 `daoheart`，OnUse 只动道心三键，不进 DamageResolver）。
- 48 行「道心境界阶」flavor 表 + 12 行「心魔」flavor 表（纯 Chronicle/显示，零数值进战力）。

**与已锁系统对齐核对**：
- **三轴解耦**：道心三键 daoHeart/innerDemon/comprehension 全进 `CultivationState.Resources`（侧表），不进 StatBlock 四维（Σ=80 不动）、不进 EffectivePower（INV-DECOUPLE corr<0.7；佛修 saveLife/命修 karmaCleared/器修 forgePerfect 等增益源为**事件驱动非 Insight 派生**，主动避共线）。
- **阈值复用**：daoFirm 门 `T_DAO_FIRM=60`、`COMP_CAP=100`、走火门 innerDemon≥80（魂修≥95）、`EPIPHANY_GATE=Insight−18`/`EPIPHANY_COOLDOWN=50`、innerDemon 危险滞回 [65 enter,50 exit] —— 全部沿用 A3-FINAL 既有常量，本库零新常量。
- **主劫 ResistTerms**：各路道心顶阶 art 注入的 (daoHeart,+w) 与 A3 §3 表逐路对齐（剑/法/阵/器/命/丹/雷=渡劫或天劫；魂/鬼/佛=心魔劫 DH×4），坐实 A3 表里「daoHeart 增益源/主劫」两列的命名落点，不改其权重。
- **§6.5 第二扇门**：12 条具名「心魔」即「引动强者心魔」（对手 innerDemon≥80 → DeviationDebuff 打折战力不扣四维）的命名内容来源，受 §6.6 五源上限约束（仅 gap≤2 可翻盘）。
- **RNG**：顿悟多结局抽样走 `cultRng=root.Split(5)`（cult 流，承 §8.3 进 World.Clone）；子流 id 含 (charId,Clock)，同种子逐字节复现。
- **九野归属**：全部命名植入江湖纪语境——道枢将裂/末劫将临下「趁灵机未尽稳道心冲境」的紧迫；大胤镇玄司忌惮走火入魔的邪修；剑冢/论道/承雷渡劫等顿悟入口皆九野地标事件，非通用玄幻。

**显式取舍（诚实标注）**：
- 道心 art 的 `tier` 上限设 t4（非 t5），刻意低于各路战力 art 的 t5/t6——道心是「稳道兜底」非「战力杠杆」，避免诱导把道心当第二战力轴堆叠（守 INV-DECOUPLE）。
- 不新增第 13 路或新 EffectOpKind/ModKind：本库纯追加数据行，落在 A schema 与 A3 常量既有能力面内（真·零改核心 L0）。若后续要把「心魔战斗打折」做成结构化 op，属 A3 §4.3/canonical gaps 已记的 DamageResolver AST 工作，不在本库 scope。

---

## 第七部 · 库间一致性自检（势力↔地名↔命名池↔历史 交叉引用无矛盾）

> 逐项核对六库交叉引用，确认 blocker/major/minor 消解后无残留矛盾。每项给「裁决依据 + 三库一致状态」。

### 7.1 魔道势力据地与属性（[blocker-C]/[major-D] 核心）
| 维度 | 势力库 | 地理库 | 历史库 | 一致状态 |
|---|---|---|---|---|
| 南下夺灵脉主体 | #10 百蛊渊魔宗·万毒堂（南疆，**Ambition=90 ResourceHunger=85**，gen0 vs 剑墟，朝廷主 War） | Region4 南疆「百蛊渊魔宗盘踞、北上夺灵脉」+ 地标 Id12「Ambition=90 ResourceHunger=85 南下夺灵脉」 | anchor201 用 (剑墟道盟,**百蛊渊魔宗**) + anchor202 朝廷 vs 剑墟忌惮 | ✅ **三库统一为百蛊渊魔宗**（canon §4.7 权威），90/85 唯一锚在百蛊渊 |
| 血河魔宫 | #11（L0 追加第二魔道，北漠·血煞渊，**Ambition=80 ResourceHunger=75 刻意低于百蛊渊**，朝廷次级警戒 -40） | 地标 **Id19 血煞渊**（北漠 Region2，绑血河魔宫(11)，Peril 75，GeoCanon 新增） | （不放今世锚点；血河魔宫非历史种子势力） | ✅ 血河魔宫据北漠（非西陲），不僭夺百蛊渊属性；据地有 GeoCanon 地标锚 |
| 西陲魔道存在 | 已消除（#10 改南疆、#11 改北漠）；#09 慕容「夹在商会与（北境）魔道之间」改「越境索取」 | Region3 西陲全商会向（Id9/10 万器 + **Id20 镔铁坞**慕容）+ **无魔窟** | — | ✅ 西陲=商会财货洼地 + 慕容坞堡，无魔道据点 |
| 慕容世家据地 | #09 西陲·镔铁坞 Id20 | 地标 **Id20 镔铁坞**（西陲 Region3，绑慕容世家(9)，坞堡 Wealth 60，GeoCanon 新增） | anchor209 世家血脉渐稀（程序生成 Clan，非特指慕容） | ✅ 镔铁坞有 GeoCanon 地标锚（[major-D] 落地） |

### 7.2 玄昊大劫主战场地理/灾劫（[major-E] 核心）
| 维度 | 地理库 | 历史库 | 一致状态 |
|---|---|---|---|
| 主战场地理 | 地标 Id8 玄昊古战场 = **北漠** Region2「主战场遗迹」 | **anchor102 改写**：`PlaceBattlefield(Grade5, Tag=北漠)`「玄昊大劫主战场在北漠」 | ✅ **主战场统一北漠**（随 GeoCanon Id8 权威） |
| 主战场灾劫 | Id8 `HazardSeed=劫烬`（§0 词表劫烬专属玄昊大劫） | **anchor102 `WriteRegionHazard(Kind=劫烬, Tag=北漠)`** | ✅ 劫烬有对应 HistorySeeder 展开来源（Id8 `HazardSeed` ←anchor102 展开） |
| 玄昊大劫本体效果 | （地理库不写 AmbientQi/恩怨，只标 HazardSeed） | anchor101「中州=**次要决战中心**·鬼雾·DampenQi18 + 全局 AmbientQi=420 地板」 | ✅ 中州降为次要决战中心（鬼雾），非主战场；全图唯一「主战场」锚点在北漠，无双主战场叙事 |
| 跨代恩怨锚 | Id8 premise 钩「跨代恩怨地理坐标」 | anchor102 `SeedAncestralGrudge(gen3, Tag=北漠)` | ✅ 北漠古战场 gen3 恩怨溯源对齐 |

### 7.3 12 路 pathId 命名一致（[minor-L] 核心）
| 库 | pathId 使用 | 一致状态 |
|---|---|---|
| 势力库 | PreferredPathKeys/ForbiddenPathKeys 全用 canon pathId（`sword_immortal/ti_xiu_hengshi/fa_xiu/gui_xiu_yang_hun/yu_shou/lei_xiu/ming_fate_causality/...`）；删 demon/music 短 key | ✅ 过 `FactionDef.Validate()` fail-fast；乐府(05)落地为 fa_xiu+lei_xiu 变体（无独立乐路） |
| 地理库 | ResourceTable 软绑路全用 canon pathId（`music_stone→fa_xiu` 音律变体注释） | ✅ 与势力库/命名库同 pathId 集 |
| 历史库 | `BindLostLineage.Tag`/`SeedBloodline.Tag` 全用 canon pathId（sword_immortal/ti_xiu_hengshi/fa_xiu/lei_xiu/yu_shou/...） | ✅ anchor103-210 path Tag 合规 |
| 命名库 | §0.5 PathId 标签、§4.1 DaoHao_Theme PathPref、§6 ArtNames 分组键全用 canon pathId（删 demon/music，补 lei_xiu/ming/soul）；INV-NAME-CANON ② 守门 | ✅ ArtNames 键 ⊆ canon 12 路 pathId 集 |
| 功法补遗库 | 12 路标题 + 道心类目全用 canon pathId | ✅ 与命名库 ArtNames 同源 |
> **canon 12 路 pathId 集（唯一权威）**：`{sword_immortal, ti_xiu_hengshi, fa_xiu, array_formation, qixiu_artificer, soul_divine_sense, ming_fate_causality, dan_xiu, gui_xiu_yang_hun, buddhist_golden_body, lei_xiu, yu_shou}`。**全六库统一用此集，无 demon/music 短 key、无短长 key 混用。**

### 7.4 势力据地 ↔ GeoCanon 地标锚点（势力库 FactionDefId ↔ 地理库 LandmarkDef.FactionDefId 双向对账）
| 势力库 # | 据地地标 | 地理库 LandmarkDef.Id | LandmarkDef.FactionDefId | 一致状态 |
|---|---|---|---|---|
| 01 大胤朝 | 神京·紫宸城/镇玄关 | Id0/Id2 | 大胤朝(1) | ✅ |
| 02 剑墟道盟 | 万剑祖庭 | Id3 | 剑墟道盟(2) | ✅ |
| 04 铁佛寺 | 伏魔禅院 | Id6 | 铁佛寺(4) | ✅ |
| 09 慕容世家 | 镔铁坞 | **Id20（新增）** | 玄铁慕容世家(9) | ✅ [major-D] |
| 10 百蛊渊魔宗 | 噬魂魔宫 | Id12 | 百蛊渊魔宗(10) | ✅ canon 南下主体 |
| 11 血河魔宫 | 血煞渊 | **Id19（新增）** | 血河魔宫(11) | ✅ [blocker-C] 北漠据地 |
| 16 万器商会 | 百炼总坛/流金商埠 | Id9/Id10 | 万器商会(16) | ✅ |
| 20 苗疆古蛊一族 | 古蛊王庭 | Id15 | 苗疆古蛊一族(20) | ✅ |
| 13 漕帮 | 姑苏烟雨坊市 | Id17 | （漕帮(13) + 程序生成） | ✅ |
> 其余势力（03/05/06/07/08/12/14/15/17/18/21/22）据地为程序锚于对应 Region（无固定地标专属），由门派 Seeder 在区内随机微 Site 落锚——符合 canon「固定骨架 + 随机微」，无矛盾。**散修登记处(19)** 全图伪势力无据地锚，符合 Rogue 特例。

### 7.5 命名池 ↔ 各库具名实体（命名库直出清单 ⊆ 各库 canon 名集）
| 命名池成分 | 直出/约束目标 | 一致状态 |
|---|---|---|
| §5.2 FactionCore（血煞/百蛊/万器/铁佛/剑墟/古蛊） | 22 具名势力固定名直出（不进随机组合） | ✅ 与势力库名册一致；血煞(扣#11 血河魔宫)、百蛊(扣#10) |
| §5.3 FactionSuffix（魔宫/鬼道/司/坞/道盟/盟） | 按 FactionType 子集约束 | ✅ 坞(扣镔铁坞 Id20)、魔宫(扣血河魔宫)、道盟(扣剑墟道盟) |
| §6 ArtNames（12 路具名功法） | ⊆ 深度设计已锁 ArtDef/CombatSkillDef 名集（INV-NAME-CANON ①） | ✅ 独孤九剑·破剑式/身外化身/大梵般若... 与功法补遗库同源 |
| §7 PlaceImagery（血煞/镔铁/百蛊/黄泉） | 地标核心词与 GeoCanon 同源 | ✅ 血煞(扣 Id19)、镔铁(扣 Id20)、百蛊(扣 Id12) |
| §7.3 命名大区直出 | 7 大区 RegionDef.Name 固定 | ✅ 中州/东海/北漠/西陲/南疆/苗疆/江南 与地理库一致 |
| §8.1 EpochName（玄昊大劫·主战场北漠） | 历史库 anchor101/102 + GeoCanon Id8 | ✅ [major-E] 主战场北漠对齐 |
| §8.2 AnchorName（剑墟·百蛊渊·百年血仇） | 历史库 anchor201 | ✅ Feud 名号与 anchor201 (剑墟道盟,百蛊渊魔宗) 一致 |

### 7.6 复仇弧种子 ↔ 戏剧引擎架构（[blocker-A] 核心）
| 维度 | 势力库 | 历史库 | 奇遇库 | 一致状态 |
|---|---|---|---|---|
| 剑墟↔百蛊渊复仇弧 | 02↔10 gen0 复仇弧种子（GrudgeKind=SectFeud，关系 -65 Feud） | anchor201 `SeedAncestralGrudge(gen2, (剑墟道盟,百蛊渊魔宗))` + `WriteFactionFeud(-75)` | 1401/1403/11001 归 **Revenge 弧**合法 Stage（Victimized/Showdown/Hunting） | ✅ **三库统一挂 `Revenge` 弧**（canon 唯一 ArcKind）；非「Encounter 弧」 |
| 非复仇桥段归属 | — | — | 1101-1303/1501-1902/11002/11003 标 **`Arc=None`（独立点火，不建弧）** | ✅ [blocker-A]：戏剧 canon 无 Encounter 弧；独立点火 storylet 不 CreateArc |
| 命名库恩怨 flavor | — | AnchorName Feud 类 | §8.3 DramaTag 5 态（结怨/蓄势/寻仇/决斗/了结）映射 Revenge 弧 5 态 | ✅ flavor 词严格映射 Revenge 弧 Victimized→BuildUp→Hunting→Showdown→Resolved/Abandoned |

### 7.7 机制算子合法性（[blocker-B]/[major-F]/[major-G]/[major-I]/[major-J] 跨库核对）
| 批判 | 库 | 消解后状态 | 一致状态 |
|---|---|---|---|
| [blocker-B] EffectKind ~18 算子自称 L0 | 奇遇库 | 诚实降级：每 effect 映射 canon 既有 chokepoint；`DramaDirector` 翻译分支 = L1 一次性核心改动 | ✅ 与 canon §戏剧「drama 层只产 DomainEvent」对齐 |
| [major-F] RealmDelta 绕 Breakthrough | 奇遇库 1302/1902/11001/1403 | 全删；破境唯一经 `EnterTribulation`→既有 Breakthrough 状态机（A2:118） | ✅ 与 A2-FINAL 升境唯一路径对齐 |
| [major-G] 注 ResistTerms / tribResist | 功法补遗库 12 顶阶 art | 改 `AddResource(daoHeart,+w)` 抬存量；删 tribResist key；零改 TribulationDef/Resolver | ✅ 与 A2:161 静态 ResistTerms fail-fast 校验对齐 |
| [major-I] 单 Amount+Tag 装多义 | 奇遇库全 effect | 附整数码字典（resKey/GrudgeKind/StatKind/flagKey 码），纳入确定性裁决排序 | ✅ Effect(Kind,From,To,Amount,Tag) 四元组可干净落地 |
| [major-J] AdjFortuneSelf(+) 单边增发 | 奇遇库 1102/1601/1602/1701/1804 等 | 配守恒源 `World.Era.UnallocFortunePool`（提议 World 字段进 Clone）整数扣减，或 TransferFortune 双边 | ✅ 与 canon INV-FORTUNE-CONSERVE 对齐 |
| [minor-K] 同节点恒真谓词 | 奇遇库 1601/1602/11003 | 改跨 RoleRef 比较（Self vs Beauty/Foe）；自身位置谓词保留（1602 Self.atSiteKind==Market） | ✅ 门控语义恢复 |
| [minor-M] RegimeWariness 无存储位 | 势力库 01↔02 / 奇遇库 1702 | = 朝廷→目标 `_relations` 语义别名，MightCache>阈 规则驱动；非独立字段 | ✅ 与 canon §4.4 Faction 字段集对齐 |
| [minor-N] AllocFortune(-40) 伪衰减 | 历史库 anchor203 | 删负值行；衰减由 `FactionLedger.DecayPct` 运行期接管（注释） | ✅ 与 canon §4.4 DecayPct 机制对齐 |

### 7.8 RngStream 占用 ↔ Clone 持久化（全库 INV-RNG-PERSIST 核对）
| 库 | 消费的 Split 流 | 消费期 | Clone 待遇 | 一致状态 |
|---|---|---|---|---|
| 势力库 | `factionRng=Split(8)` | 运行期（夺地/气运转移端点） | **进 World.Clone**（升 World 字段，§8.3） | ✅ |
| 地理库 | `mapRng=Split(7)`（随机微 Site） | 运行期（秘境周期/资源 Reserve） | **进 World.Clone** | ✅ |
| 历史库 | `genRng.Split(GEN_HISTORY=101)` | **生成期一次性** | 不进 Clone（跳号派生不消费 root） | ✅ |
| 奇遇库 | `dramaRng=Split(6)` + 渡劫 `cultRng=Split(5)` | 运行期 | **均进 World.Clone**；`UnallocFortunePool`(提议 World 字段)进 Clone | ✅ [major-J] |
| 命名库 | `genRng.Split(GEN_NAMING=102)` | **生成期一次性** | 不进 Clone（运行期不消费） | ✅ [区别于 map7/faction8/cult5] |
| 功法补遗库 | 顿悟 `cultRng=Split(5)` | 运行期 | **进 World.Clone** | ✅ |
> **顶层 Split 号占用核对**（canon §8.2 冻结 1-8）：势力库占 8、地理库占 7、奇遇库占 6+5、功法补遗占 5；历史库/命名库走 `Split(1)` 命名子流（101/102），**不开新顶层号**。全库无顶层 Split 号冲突。

### 7.9 残留显式取舍（诚实标注，非矛盾但需登记）
- **L1 一次性核心改动（非 L0）**：(a) 奇遇库 `ArcKind.None` 哨兵枚举追加（或 `DramaDirector`「standalone 不建弧」分支）；(b) 奇遇库 `DramaDirector` 的「storylet 意图码→DomainEvent→chokepoint」翻译表新增分支（~18 意图）；(c) `World.Era.UnallocFortunePool` 新增 World 字段 + 进 Clone（[major-J] 守恒源）。**这三项是本附录依赖的一次性核心支持，诚实标 L1；此后加 storylet/势力/锚点/功法数据行才是 L0。**
- **乐修无独立 pathId**：势力库焚天乐府(05)落地为 `fa_xiu`(音律法术)+`lei_xiu`(烈阳音律)变体，命名库 music_stone 软绑 `fa_xiu`——canon 12 路无独立乐路，本附录不为乐修发明第 13 路（守 canon），乐府以现有路变体承载，flavor 命名仍可「焚香玉册/听潮山」九野化。
- **血河魔宫属性刻意低配**：#11 Ambition=80/ResourceHunger=75 < 百蛊渊 90/85，是为「不僭夺 canon 主体 + 体现旁支竞争者」的显式数值取舍，非平衡定论（待 §8 标定）。

### 可程序化锚点（一致性自检）
- **交叉引用键**：势力 `FactionDefId` ↔ 地标 `LandmarkDef.FactionDefId`（双向单一来源）；path `pathId`（canon 12 路集，六库唯一）；地标 Id（NodeId 连续空间）；锚点 Tag（RegionId/FactionDefId/pathId 码）。
- **gate**：INV-NAME-CANON（命名⊆canon 名集）/ INV-GEO-DECOUPLE（地图不引用 Faction，势力单向读地标）/ INV-RNG-PERSIST（运行期流进 Clone）/ INV-FORTUNE-CONSERVE（气运守恒）/ INV-DECOUPLE（道心三轴正交）。
- **L0/L1 分级**：六库内容扩展（加势力/地标/锚点/storylet 数据行/功法行）= L0；7.9 登记的三项 = L1 一次性核心支持（诚实标注）。

---

> **本附录性质重申**：机制以 World Bible canonical（`2026-06-13-WorldBible-九野-canonical.md`）+ v1.2-A/A2/A3/B/C 各 design 为唯一权威；本附录只填具体内容（数据行/表格）。全部 14 条批判（blocker×3 / major×8 / minor×3）已在头部索引钉死、正文逐处落地、第七部交叉核对，无残留库间矛盾。L1 一次性核心改动（ArcKind.None 哨兵 / DramaDirector 翻译表 / UnallocFortunePool 字段）已诚实标注，不谎称 L0。

---

**附录交付完成。关键产物文件路径（绝对路径）**：
- 本附录所依据的 canonical 唯一权威：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-WorldBible-九野-canonical.md`
- 12 路深度（功法名/道心机制/ResistTerms 来源）：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\research\2026-06-13-v1.2-A-修炼路线-每路深度设计.md`
- 戏剧引擎（ArcKind{Revenge} 唯一/5 态机，[blocker-A] 依据）：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-v1.2-B-戏剧引擎-design.md`
- 修炼三轴/Breakthrough 状态机（[major-F] RealmDelta 删除依据）：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-v1.2-A2-修炼大小境界与全流程-FINAL-design.md`
- 破单调奇遇闭关道心（[major-G] ResistTerms 静态表依据）：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-v1.2-A3-破单调奇遇闭关道心-FINAL-design.md`