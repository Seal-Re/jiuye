namespace Jianghu.Cultivation
{
    /// <summary>
    /// 战斗效果模块工厂（B5 模块化效果系统·单一构造入口，CLAUDE.md B 技术红线）。
    /// 21 路招牌招/功法的 OnUse/OnDefend 模块**一律经此构造**，禁裸 <c>new EffectOp(七参)</c> 散写——
    /// 把 ratio-Kind 的 <c>Amount2≥1</c>（§15.6）、Reflect 的 <c>Trigger=OnDefend</c>（§15.5）、
    /// 稀有档 <c>Rarity</c> 等易漏参数收敛到工厂一处保证，杜绝逐路手写漏参。
    ///
    /// <para>积木分两层（与设计 §7 对应）：本工厂 = 普通/稀有档有限算子集；
    /// 唯一档签名机制走 <see cref="SpecialModuleRegistry"/> 注册式插件。新算子 = 加一个工厂方法
    /// + <see cref="ModuleResolver"/> 一个分支（可插拔），不改既有积木。</para>
    ///
    /// 纯整数、确定性、无 RNG。每积木 XML 标注语义 + 所属 spec 条目。
    /// </summary>
    public static class Modules
    {
        // ———————————————————————————— 普通档（Common）————————————————————————————

        /// <summary>固定破防：dmg += amount。最基础积木（剑气如虹/破御剑等定值穿透）。</summary>
        public static EffectOp FlatPen(int amount, string? note = null)
            => new EffectOp(EffectOpKind.AddPenInteger, null, amount, note);

        /// <summary>固定减伤（OnDefend 基线护体）：dmg -= amount。</summary>
        public static EffectOp FlatDR(int amount, string? note = null)
            => new EffectOp(EffectOpKind.AddFlatDR, null, amount, note,
                Trigger: EffectTrigger.OnDefend);

        // ———————————————————————————— 稀有档（Rare）————————————————————————————

        /// <summary>
        /// 资源转伤（ratio-Kind）：dmg += res(resKey) × mul / div。资源越满越痛、见底哑火（真差分）。
        /// <paramref name="div"/> 自动钳 ≥1（§15.6 消 Amount2=0 双义）。剑意/血气/法术库广度等转伤主力积木。
        /// </summary>
        public static EffectOp PenFromResource(string resKey, int mul, int div = 1, string? note = null)
            => new EffectOp(EffectOpKind.PenFromResource, resKey, mul, note,
                Amount2: div < 1 ? 1 : div, Rarity: EffectRarity.Rare);

        /// <summary>
        /// tag 克制倍乘（ratio-Kind）：防方带 <paramref name="tag"/> → dmg × num/den（联合上界 ×3/2，§15.4）；
        /// 否则不变。<paramref name="den"/> 自动钳 ≥1。破阴邪（CounterMul evil）、克邪等克制积木。
        /// </summary>
        public static EffectOp CounterMul(string tag, int num, int den = 1, string? note = null)
            => new EffectOp(EffectOpKind.CounterMul, tag, num, note,
                Amount2: den < 1 ? 1 : den, Rarity: EffectRarity.Rare);

        /// <summary>
        /// 群攻（每目标）：R2 单挑退化 = dmg + amount；群战按敌数放大（万剑归宗/AoE 阵法）。
        /// </summary>
        public static EffectOp AoePerTarget(int amount, string? note = null)
            => new EffectOp(EffectOpKind.AoePerTarget, null, amount, note, Rarity: EffectRarity.Rare);

        /// <summary>
        /// 反噬自伤：条件 <paramref name="condKey"/> 满足 → 攻方自伤 <paramref name="selfDmg"/>
        /// （selfDmg 通道批4 接，本轮 ApplyOnUse 不改入伤）。舍身一剑/承雷自险/血符等高风险积木。
        /// </summary>
        public static EffectOp Backlash(string condKey, int selfDmg, string? note = null)
            => new EffectOp(EffectOpKind.Backlash, condKey, selfDmg, note, Rarity: EffectRarity.Rare);

        /// <summary>
        /// 夺取资源（chokepoint）：防方 res(resKey) -= amount、攻方 += amount（经 [Min,Cap] 钳）；dmg 不变。
        /// 噬尽夺元/夺定数/夺运等掠夺积木。
        /// </summary>
        public static EffectOp Drain(string resKey, int amount, string? note = null)
            => new EffectOp(EffectOpKind.DrainResource, resKey, amount, note, Rarity: EffectRarity.Rare);

        /// <summary>
        /// 持续伤（OnUse 挂载，回合间结算批4 接）：防方挂 <paramref name="perTick"/>/tick ×
        /// <paramref name="turns"/> 回合。瘟疫毒雾等 dot 积木。
        /// </summary>
        public static EffectOp Dot(string poisonKey, int perTick, int turns, string? note = null)
            => new EffectOp(EffectOpKind.Dot, poisonKey, perTick, note,
                Amount2: turns, Rarity: EffectRarity.Rare);

        /// <summary>
        /// 控场（OnUse 挂载，回合间结算批4 接）：防方下 <paramref name="turns"/> 回合 selectMove 失效。
        /// 困龙/律狱/勾魂等控场积木。
        /// </summary>
        public static EffectOp Control(string ctrlKey, int turns, string? note = null)
            => new EffectOp(EffectOpKind.Control, ctrlKey, turns, note, Rarity: EffectRarity.Rare);

        /// <summary>
        /// 反伤（ratio-Kind，OnDefend）：攻方受 incoming × num/den（读扣血前/不递归，§15.5；时序结算批4 接）。
        /// <paramref name="den"/> 自动钳 ≥1，Trigger 固定 OnDefend。铁山靠/护宝罡等反震积木。
        /// </summary>
        public static EffectOp Reflect(int num, int den = 1, string? note = null)
            => new EffectOp(EffectOpKind.ReflectDamage, null, num, note,
                Amount2: den < 1 ? 1 : den, Trigger: EffectTrigger.OnDefend, Rarity: EffectRarity.Rare);

        /// <summary>
        /// 闪避（OnDefend，连续缩放）：减伤 = clamp((身法-命中+amount)×k, 0, maxReduce)（§15.2）。
        /// 功法门控防御积木（无功法→能力 0，M5）。
        /// </summary>
        public static EffectOp Evade(int amount, string? note = null)
            => new EffectOp(EffectOpKind.Evade, null, amount, note, Trigger: EffectTrigger.OnDefend);

        // —— 唯一档（Unique）签名机制走 SpecialModuleRegistry 注册式插件 ——

        /// <summary>
        /// 唯一档签名机制（§7 M3 逃逸口）：Key=handlerId，<see cref="ModuleResolver"/> 派发
        /// <see cref="SpecialModuleRegistry"/>[handlerId].Apply(ctx,op) → 伤害 delta + chokepoint 副作用。
        /// 落宝/炸阵/夺舍/金身态/律场总门等独门机制。handler 纪律：纯整数 / 不读 daoHeart / 不掷随机 / 副作用经 chokepoint。
        /// <paramref name="amount"/>/<paramref name="amount2"/> 供 handler 读参（如阶数/回合数）。
        /// </summary>
        public static EffectOp Special(string handlerId, int amount = 0, int amount2 = 0, string? note = null)
            => new EffectOp(EffectOpKind.Special, handlerId, amount, note,
                Amount2: amount2, Rarity: EffectRarity.Unique);
    }
}
