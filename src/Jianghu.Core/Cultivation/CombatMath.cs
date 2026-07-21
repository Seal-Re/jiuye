using System;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// combat-variance cv-001（adr-0008 决策②）：Margin→关键事件触发概率的**整数查表**映射。
    /// 禁浮点 Sigmoid（B.2 IL 扫描守）——形状仿 Sigmoid 的整数 permille 桶表，落地为查表。
    ///
    /// 语义：<see cref="GetSuccessPermille"/> 返回攻方"命中/有效伤害"的千分比概率 p∈[1,999]，
    /// 由攻防 PE 的**相对差**（peMargin 相对 defenderPe 的百分比）分桶决定。同 PE→500(50/50 悬念)；
    /// 碾压→趋 999（保 1‰ 理论反杀，弱胜强叙事地基）；被碾压→趋 1。
    ///
    /// 纯整数、纯函数、无 RNG、无副作用——单测直验，确定性（B.2）。
    /// 桶边界/概率值仿 adr-0008 附录A（示例形状）；最终 [40,60]% 硬闸门标定在 cv-005（seed-sweep）。
    /// </summary>
    public static class CombatMath
    {
        /// <summary>同 PE（margin=0）基准命中率：50%。</summary>
        public const int BasePermille = 500;

        /// <summary>概率下钳（极端劣势仍保 1‰ 理论反杀，adr-0008 α "弱胜强地基"）。</summary>
        public const int MinPermille = 1;

        /// <summary>概率上钳（极端优势仍留 1‰ 失手，非绝对——绝对秒杀走 cv-004 溢出/auto-win）。</summary>
        public const int MaxPermille = 999;

        /// <summary>
        /// cv-006（adr-0010 决策①）：SEC==0「必中标签」返回值 = 1000。
        /// 故意大于 <see cref="MaxPermille"/>（999）：cv-001 伯努利判定为 <c>roll &lt; p</c>（roll∈[0,999]），
        /// 故 p=1000 使 <c>roll &lt; 1000</c> 恒真 → 真·必中（无法闪避）。承 adr-0008 ⑨.2「permille≥1000 = 不可闪避」。
        /// **不重定义 <see cref="MaxPermille"/>**——后者是 cv-001 <see cref="GetSuccessPermille"/> 的钳制上界，
        /// 属 byte-identical 基线（AC 6.4 / G.2），cv-006 不得擅改。
        /// </summary>
        public const int AutoHitPermille = 1000;

        /// <summary>
        /// 攻方 PE − 防方 PE = <paramref name="peMargin"/>；<paramref name="defenderPe"/> = 防方战力（作相对基准）。
        /// 返回攻方命中千分比 p∈[<see cref="MinPermille"/>,<see cref="MaxPermille"/>]。
        ///
        /// 相对差（scale-invariant，跨 UT 通用）：relPct = peMargin×100 / max(1,defenderPe)。
        /// 分桶（仿 adr-0008 附录A）：每 1% 相对差 → ±5‰，即 permille = 500 + relPct×5，再钳 [1,999]。
        /// relPct≥100（碾压，攻方≥防方2倍）→ 上钳 999；relPct≤−100 → 下钳 1。
        /// </summary>
        public static int GetSuccessPermille(int peMargin, int defenderPe)
        {
            // 防 0 除 + 负 PE 兜底（PE 恒 ≥0，但 defenderPe=0 时用 1 作基准避免除零）。
            int basis = defenderPe > 0 ? defenderPe : 1;

            // 相对差百分比（整数）：先钳 peMargin 防 int 溢出（|peMargin| 上限取 basis×100 足够覆盖 [−100%,+100%]）。
            long clampedMargin = Math.Clamp((long)peMargin, -(long)basis * 100, (long)basis * 100);
            int relPct = (int)(clampedMargin * 100 / basis);

            // 每 1% 相对差 → 5‰ 偏移（仿附录A：±20%→±100‰、±60%→±300‰、±100%→±500‰... 此处线性桶）。
            long permille = BasePermille + (long)relPct * 5;

            return (int)Math.Clamp(permille, MinPermille, MaxPermille);
        }

        /// <summary>
        /// cv-006（adr-0010 决策①）：闪避系数 SEC 合流——对 cv-001 基础命中 permille <paramref name="p"/>
        /// 作**前置整数调制**，不新增掷骰（承「单次交锋单次核心判定」）。纯整数、纯函数、无 RNG（B.2）。
        ///
        /// 语义：SEC = 攻方招式的闪避系数（permille 基准，挂 <c>CombatSkillDef.Sec</c>）。
        /// - SEC=1000（中性）→ <paramref name="p"/> 不变（21 路默认，惰性零行为改变，AC 6.4）。
        /// - SEC=0（必中标签）→ 返回 <see cref="AutoHitPermille"/>（1000），使 cv-001 伯努利 <c>roll&lt;1000</c> 恒真 → 真·必中。
        ///   显式分支（用户裁定：抛弃 <c>max(1,SEC)</c> 数学小聪明，避免除零 Code Smell）。
        /// - SEC&gt;1000（易闪）→ <c>p*1000/SEC</c> 命中衰减（SEC=2000 → 半衰）。
        /// - SEC&lt;1000 且 &gt;0（难闪）→ <c>p*1000/SEC</c> 命中抬升（SEC=500 → 2×）。
        ///
        /// 边界裁定（QA AC-2）：
        /// - <paramref name="p"/>&lt;=0 → 返回 0（攻方零基础命中，SEC 无从修正；"p=0 任意 SEC 仍为 0"）。此守卫**前置**，
        ///   故 <c>Apply(0, 0)=0</c>（零命中不被 SEC=0 救活），而 <c>Apply(p&gt;0, 0)=1000</c>（必中）。生产路径 p∈[1,999]，
        ///   SEC=0 恒为必中。
        /// - 结果钳 ≤<see cref="AutoHitPermille"/>（1000）——满足 QA AC-2「clamped to 1000」。下游 <c>roll&lt;p</c>
        ///   对钳/不钳行为等价（cv-006 Core-only，无 >1000 溢出消费者；cv-008 SBC 才碰 Chip 穿透）。
        /// - <paramref name="sec"/>&lt;0（病态）→ 防御性下钳 0（生产 SEC 恒 ≥0，仅守护栏）。
        /// </summary>
        /// <param name="p">cv-001 基础命中 permille（<see cref="GetSuccessPermille"/> 输出，生产 ∈[1,999]）</param>
        /// <param name="sec">攻方招式闪避系数（<c>CombatSkillDef.Sec</c>；1000=中性 / 0=必中 / &gt;1000=易闪）</param>
        /// <returns>调制后命中 permille（∈[0, <see cref="AutoHitPermille"/>]）</returns>
        public static int ApplyEvasionCoefficient(int p, int sec)
        {
            // 守卫 1：攻方零基础命中 → SEC 无从修正（QA AC-2 "p=0 任意 SEC 仍为 0"）。前置于 SEC==0 必中分支。
            if (p <= 0) return 0;
            // 守卫 2：SEC==0 必中标签 → 显式分支返回 AutoHitPermille(1000)，不进除法（用户裁定，adr-0010 决策①）。
            if (sec == 0) return AutoHitPermille;
            // 病态负 SEC（生产恒 ≥0）：防御性归 0，避免负 permille 进下游。
            if (sec < 0) return 0;
            // 主路径：整数调制 p*1000/SEC（B.2 向下取整）。long 中间量防 p*1000 溢出（p≤999 → ≤999000，int 安全，但 long 留余量）。
            long adjusted = (long)p * 1000 / sec;
            if (adjusted < 0) return 0;
            // 钳 ≤1000（QA AC-2 "clamped to 1000"；SEC<1000 抬升时 p*1000/SEC 可能 >1000，如 Apply(600,500)=1200 → 1000）。
            return adjusted > AutoHitPermille ? AutoHitPermille : (int)adjusted;
        }

        /// <summary>
        /// cv-004（adr-0008 决策⑨.2）：检测 p_permille 是否达到溢出阈值。
        /// 溢出时 NPC 侧跳过伯努利掷骰（数学必中——roll&lt;1000 恒真）。
        /// 纯函数，无 RNG（B.2）。
        /// </summary>
        public static bool IsOverflow(int p, int threshold) => p >= threshold && threshold > 0;

        /// <summary>
        /// cv-008（adr-0010 决策②）：格挡系数 SBC 调制——对 cv-003 基准 Chip 穿透千分比
        /// <paramref name="baseChipPermille"/> 作**确定性整数调制**，生成有效 Chip 穿透千分比。
        /// 纯整数、纯函数、无 RNG（B.2）。格挡本身是否成功由 cv-003 Block 类模块（FlatDR/ReflectDamage）
        /// **确定性**决定（动能维度对撞，不掷骰）；SBC 只调制格挡**成功后**的 Chip 穿透比例。
        ///
        /// 语义：SBC = 攻方招式的格挡系数（permille 基准，挂 <c>CombatSkillDef.Sbc</c>）。
        /// - SBC=1000（中性）→ <paramref name="baseChipPermille"/> 不变（21 路默认，惰性零行为改变，AC 8.1）。
        /// - SBC=0（不可格挡穿透）→ 返回 1000（全伤穿透，比照 cv-003 Blunt 门控语义，adr-0010 决策②）。
        ///   显式分支（同 SEC=0 范式：抛弃 <c>max(1,SBC)</c> 数学小聪明，避免除零 Code Smell）。
        /// - SBC&gt;1000（易格挡）→ <c>baseChipPermille*1000/SBC</c> 降低穿透（SBC=2000 → 半降）。
        /// - SBC&lt;1000 且 &gt;0（重锤/钝器）→ <c>baseChipPermille*1000/SBC</c> 抬升穿透（SBC=500 → 2×）。
        ///
        /// 边界裁定（QA AC-2）：
        /// - <paramref name="baseChipPermille"/>&lt;=0 → 返回 0（无 chip 基准可调制；与 <see cref="ChipDamageFloor"/>
        ///   chipPermille≤0 退化语义一致——无 chip 基准则无穿透）。
        /// - SBC==0 必中全伤 → 返回 1000（**不**返回 <paramref name="baseChipPermille"/>；不可格挡 = 全伤，非中性）。
        /// - SBC&lt;0（病态）→ 防御性返回 <paramref name="baseChipPermille"/>（中性，生产 SBC 恒 ≥0，仅守卫栏）。
        /// - **不钳上界**：低 SBC（重锤）可使 effChip &gt; 1000（如 SBC=300 → 1000），这是"破防"语义；
        ///   下游 <see cref="ChipDamageFloor"/> 取 <c>max(dmg, chipFloor)</c> 自然约束，且有 int.MaxValue 回绕守卫。
        /// </summary>
        /// <param name="baseChipPermille">cv-003 基准 Chip 穿透千分比（<see cref="LimitsConfig.ChipDamagePermille"/>，生产=300）</param>
        /// <param name="sbc">攻方招式格挡系数（<c>CombatSkillDef.Sbc</c>；1000=中性 / 0=不可格挡全伤 / &lt;1000=重锤 / &gt;1000=易格挡）</param>
        /// <returns>调制后有效 Chip 穿透千分比（SBC=0→1000；SBC&gt;0→baseChipPermille*1000/max(1,SBC)）</returns>
        public static int ApplyBlockCoefficient(int baseChipPermille, int sbc)
        {
            // 守卫 1：无 chip 基准 → 0（与 ChipDamageFloor chipPermille≤0 退化一致，不凭空造穿透）。
            if (baseChipPermille <= 0) return 0;
            // 守卫 2：SBC==0 不可格挡穿透 → 显式分支返回 1000（全伤），不进除法（adr-0010 决策②，比照 cv-003 Blunt 门控）。
            if (sbc == 0) return 1000;
            // 病态负 SBC（生产恒 ≥0）：防御性归中性，避免负 permille 进下游。
            if (sbc < 0) return baseChipPermille;
            // 主路径：整数调制 baseChipPermille*1000/max(1,SBC)（B.2 向下取整）。
            // long 中间量防 baseChipPermille*1000 溢出（baseChipPermille≤int.MaxValue → *1000 需 long）。
            // max(1,SBC) 防 SBC=0 已被上面分支截走后的病态极小正值（SBC=1 → 不溢出，effChip=baseChipPermille*1000）。
            long adjusted = (long)baseChipPermille * 1000 / Math.Max(1, sbc);
            if (adjusted < 0) return 0;
            // 不钳上界（低 SBC 重锤可 >1000，破防语义）；防 int 回绕（病态大 baseChipPermille）。
            return adjusted > int.MaxValue ? int.MaxValue : (int)adjusted;
        }

        /// <summary>
        /// cv-007（adr-0010 决策③）：派生抗性 R 的半衰减伤——对 RawDamage 做
        /// <c>DamageMultiplier = K×1000/(K+R)</c> 整数衰减，<c>max(1, ...)</c> 保底。纯整数、纯函数、无 RNG（B.2）。
        ///
        /// 语义：R=0 → multiplier=1000（无减伤，全伤）；R=K → multiplier=500（半衰，50% 减伤）；
        /// R→∞ → multiplier→0（趋 1 保底，永不归零）。R 是派生属性（体质/识/功法标签映射，见 <see cref="ResistanceProviders"/>），
        /// **禁进 EffectivePower**（B.5：抗性只作防御结算，不算战力）。
        ///
        /// 边界裁定（QA AC-2）：
        /// - <paramref name="rawDamage"/>&lt;=0 → 返回 0（无伤害可衰减；与 DuelEngine 上游 max(0,...) 一致，不凭空造伤害）。
        /// - <paramref name="R"/>&lt;0（病态）→ 视为 0（生产 R 恒 ≥0，防御性钳制，避免负 R 抬伤害）。
        /// - <paramref name="K"/>&lt;=0 → 视为无衰减（返回 rawDamage；生产 K 恒 >0 经 <see cref="LimitsConfig.Validate"/> 守，此处仅守卫栏）。
        /// - 极大 R（如 100000）→ multiplier→0 → max(1,...) 保底返 1。
        /// - <paramref name="rawDamage"/>=1 任意 R → 1（保底不归零）。
        /// </summary>
        /// <param name="rawDamage">衰减前伤害（未缩放空间，与 DuelEngine dmg/Scale 一致）</param>
        /// <param name="R">派生抗性分值（<see cref="ResistanceProviders.ResistanceOf"/> 输出，≥0）</param>
        /// <param name="K">半衰常数（<see cref="LimitsConfig.ResistanceHalfLifeK"/>，>0；R=K 时半衰）</param>
        /// <returns>衰减后伤害（≥1 当 rawDamage>0；0 当 rawDamage≤0）</returns>
        public static int ApplyResistance(int rawDamage, int R, int K)
        {
            // 守卫 1：无伤害可衰减 → 0（不凭空造伤害，与上游 max(0,...) 一致）。
            if (rawDamage <= 0) return 0;
            // 守卫 2：病态 K（生产恒 >0 经 Validate 守）→ 无衰减，返原值。
            if (K <= 0) return rawDamage;
            // 守卫 3：病态负 R（生产恒 ≥0）→ 视为 0（无抗性），避免负 R 抬伤害。
            int rEff = R < 0 ? 0 : R;
            // 半衰乘子 = K×1000/(K+R)（B.2 整数向下取整）。long 中间量防 K*1000 溢出（K≤int.MaxValue → K*1000 需 long）。
            long mul = (long)K * 1000 / (K + rEff);
            // 衰减后伤害 = rawDamage × mul / 1000（long 中间量防 rawDamage×mul 溢出）。
            long dmg = (long)rawDamage * mul / 1000;
            // max(1, ...) 保底：rawDamage>0 时衰减后不低于 1（R 极大趋 0 不归零，adr-0010 决策③）。
            if (dmg < 1) return 1;
            return dmg > int.MaxValue ? int.MaxValue : (int)dmg; // 防 int 回绕（病态大输入）
        }
    }
}
