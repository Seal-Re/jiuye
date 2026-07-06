using System;
using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Cultivation.Artifacts;
using Jianghu.Model;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// R2 战斗结算引擎（story-003 batch4 + fullstruct-007 rollback stack）。接模块系统：HP=pe、选招经 tier/cost/gate、
    /// OnUse→OnDefend→软情境同时扣血。回合间 push RollbackStack 快照，支持因果逆演/夺舍续命的
    /// 结算回滚。纯整数、确定性、无 RNG。
    /// off（无 Cultivation）不入本引擎，由 SparAction 走 legacy（红线 B.3）。
    /// </summary>
    public static class DuelEngine
    {
        /// <summary>基础伤害除数：pe / DIVISOR = 每回合基础伤害量。</summary>
        public const int BaseDamageDivisor = 10;

        /// <summary>最大战斗回合数（防无限循环）。</summary>
        public const int MaxRounds = 20;

        /// <summary>
        /// R2 战斗结算结果。Winner/Loser/Margin 承现有 DuelResolved 契约。
        /// </summary>
        public sealed record Result(
            CharacterId Winner, CharacterId Loser, int Margin,
            bool WasAutoWin, int AttackerHpRemaining, int DefenderHpRemaining,
            IReadOnlyDictionary<string, int>? AttackerStatDeltas = null,
            IReadOnlyDictionary<string, int>? DefenderStatDeltas = null,
            int AttackerRelationDelta = 0,
            int DefenderRelationDelta = 0);

        /// <summary>
        /// 双方经模块系统战斗（story-003 batch4）。
        /// Step 1: UT gap check（AC 4.4）→ auto-win。
        /// Step 2: HP=pe；选招（AC 4.1）；造 CombatContext。
        /// Step 3: 每回合双方同时互攻——OnUse→OnDefend→软情境→同时扣血（AC 4.2）。
        /// Step 4: 胜者 HP 高；平 Tiebreak(CharacterId)（AC 4.6）。
        /// </summary>
        /// <param name="attacker">攻方角色</param>
        /// <param name="defender">防方角色</param>
        /// <param name="attackerPath">攻方修炼路定义</param>
        /// <param name="defenderPath">防方修炼路定义</param>
        /// <param name="registry">路线注册表（用于对手 tag 查询）</param>
        /// <param name="limits">配置限制</param>
        /// <param name="resolver">软情境结算器（可空=无情境 adj）</param>
        /// <param name="attackerSkill">攻方所选战技（可空=裸攻，无 OnUse 模块）</param>
        /// <param name="defenderSkill">防方所选战技（可空=裸攻，无 OnUse 模块）</param>
        /// <param name="attackerArtifact">攻方装备法宝（可空）</param>
        /// <param name="defenderArtifact">防方装备法宝（可空）</param>
        public static Result ResolveR2(
            Character attacker, Character defender,
            CultivationPathDef attackerPath, CultivationPathDef defenderPath,
            PathRegistry registry, LimitsConfig limits,
            SituationalResolver? resolver,
            CombatSkillDef? attackerSkill, CombatSkillDef? defenderSkill,
            ArtifactDef? attackerArtifact = null, ArtifactDef? defenderArtifact = null,
            bool calibrationMode = false)
        {
            if (attacker.Cultivation == null || defender.Cultivation == null)
                throw new ArgumentException("DuelEngine.ResolveR2 requires both sides have Cultivation (off→legacy SparAction)");

            // —— AC 4.4：UT 差≥2 → 高 UT 直判胜 ——
            int utA = UnifiedTier(attacker.Cultivation, attackerPath);
            int utB = UnifiedTier(defender.Cultivation, defenderPath);
            if (Math.Abs(utA - utB) >= 2)
            {
                bool aWins = utA > utB;
                return new Result(
                    aWins ? attacker.Id : defender.Id,
                    aWins ? defender.Id : attacker.Id,
                    int.MaxValue,
                    WasAutoWin: true,
                    AttackerHpRemaining: aWins ? 1 : 0,
                    DefenderHpRemaining: aWins ? 0 : 1);
            }

            // —— AC 4.1：HP = pe ——
            int peA = PowerEngine.Evaluate(attacker.Cultivation, attacker.Stats, attackerPath, limits);
            int peB = PowerEngine.Evaluate(defender.Cultivation, defender.Stats, defenderPath, limits);
            int hpA = peA, hpB = peB;

            // —— 造 CombatContext ——
            var ctx = new CombatContext(attacker.Cultivation, attackerPath, defender.Cultivation, defenderPath);

            // —— AC 4.1：选招（传入技能优先；未传入则自动选最优）——
            var aSkill = attackerSkill ?? SelectBestSkill(attacker.Cultivation, attackerPath);
            var dSkill = defenderSkill ?? SelectBestSkill(defender.Cultivation, defenderPath);
            aSkill = ValidateSkill(aSkill, attacker.Cultivation, attackerPath);
            dSkill = ValidateSkill(dSkill, defender.Cultivation, defenderPath);

            // —— AC 4.2：回合循环，双方同时互攻。每回合 push RollbackStack 快照（fullstruct-007）——
            int roundLimit = MaxRounds;
            long totalDmgToB = 0, totalDmgToA = 0;
            var pendingDots = new List<DotEntry>();
            var pendingControls = new List<ControlEntry>();
            var controlLimiter = new ControlLimiterState(); // balance-007：CD/DR duel-local
            var rollbackStack = new RollbackStack();
            for (int round = 0; round < roundLimit && hpA > 0 && hpB > 0; round++)
            {
                // 被控方 selectMove 失效: skill→null, dmg=0, 不施dot/control
                bool defenderControlled = IsControlled(pendingControls, Side.Defender);
                bool attackerControlled = IsControlled(pendingControls, Side.Attacker);
                var activeASkill = attackerControlled ? null : aSkill;
                var activeDSkill = defenderControlled ? null : dSkill;

                int effPeA = ctx.ApplyEPModifiers(Side.Attacker, peA);
                int effPeB = ctx.ApplyEPModifiers(Side.Defender, peB);

                // —— 批4：guRevolt 阵营反噬 → 攻方伤害重定向到自身 ——
                bool aRedirected = IsGuRevoltRedirected(ctx, Side.Attacker);
                bool bRedirected = IsGuRevoltRedirected(ctx, Side.Defender);

                // —— 批4：residualOrder 耗尽 → 军团僵死（该方无法行动） ——
                bool aFleetFrozen = ctx.HasResource(Side.Attacker, "residualOrder")
                    && !HasResidualOrder(ctx, Side.Attacker);
                bool bFleetFrozen = ctx.HasResource(Side.Defender, "residualOrder")
                    && !HasResidualOrder(ctx, Side.Defender);

                // —— Exchange 1: 攻方→防方 ——
                var (rawDmgToB, reflectToA) = ResolveExchange(
                    activeASkill, effPeA, defender.Cultivation,
                    attackerPath, defenderPath, ctx, limits, resolver,
                    Side.Attacker, Side.Defender,
                    pendingDots, pendingControls, controlLimiter, round,
                    attackerArtifact, defenderArtifact, calibrationMode);

                int dmgToB, dmgToA_redirect;
                if (attackerControlled || aFleetFrozen)
                { dmgToB = 0; dmgToA_redirect = 0; }
                else if (aRedirected)
                { dmgToB = 0; dmgToA_redirect = rawDmgToB; } // 反噬：自伤
                else
                { dmgToB = rawDmgToB; dmgToA_redirect = 0; }

                // —— Exchange 2: 防方→攻方 ——
                var (rawDmgToA, reflectToB) = ResolveExchange(
                    activeDSkill, effPeB, attacker.Cultivation,
                    defenderPath, attackerPath, ctx, limits, resolver,
                    Side.Attacker, Side.Defender,
                    pendingDots, pendingControls, controlLimiter, round,
                    defenderArtifact, attackerArtifact, calibrationMode);

                int dmgToA, dmgToB_redirect;
                if (defenderControlled || bFleetFrozen)
                { dmgToA = 0; dmgToB_redirect = 0; }
                else if (bRedirected)
                { dmgToA = 0; dmgToB_redirect = rawDmgToA; } // 反噬：自伤
                else
                { dmgToA = rawDmgToA; dmgToB_redirect = 0; }

                // Merge redirect damage
                dmgToA += dmgToA_redirect;
                dmgToB += dmgToB_redirect;

                // —— fullstruct-007：push 交锋前快照到回滚栈 ——
                rollbackStack.Push(new ExchangeSnapshot(
                    hpA, hpB, dmgToB, dmgToA, reflectToB, reflectToA,
                    totalDmgToB, totalDmgToA));

                // 同时扣血（读 pre-HP）：反伤加到对应方向
                hpB = Math.Max(0, hpB - dmgToB - reflectToB);
                hpA = Math.Max(0, hpA - dmgToA - reflectToA);
                totalDmgToB += dmgToB + reflectToB;
                totalDmgToA += dmgToA + reflectToA;

                // —— fullstruct-007：检查回滚信号 ——
                if (ctx.PendingRollback == RollbackSignal.ReverseRequested)
                {
                    // 因果逆演：撤销刚结算的一次交锋（伤害/夺运/胜负回滚）
                    var prev = rollbackStack.Pop();
                    if (prev.HasValue)
                    {
                        hpA = prev.Value.AttackerHpBefore;
                        hpB = prev.Value.DefenderHpBefore;
                        totalDmgToB = prev.Value.TotalDmgToBBefore;
                        totalDmgToA = prev.Value.TotalDmgToABefore;
                    }
                }
                else if (ctx.PendingRollback == RollbackSignal.PossessionRequested && hpA <= 0)
                {
                    // 夺舍续命：濒死触发 → 回滚上次致命伤害 → HP 恢复
                    // Gate：对手含 thunder/pure_yang/buddha_light 标签 → 夺舍被压制
                    if (!CheckPossessionGate(defenderPath))
                    {
                        var prev = rollbackStack.Pop();
                        if (prev.HasValue)
                        {
                            hpA = prev.Value.AttackerHpBefore;
                            hpB = prev.Value.DefenderHpBefore;
                            totalDmgToB = prev.Value.TotalDmgToBBefore;
                            totalDmgToA = prev.Value.TotalDmgToABefore;
                        }
                    }
                }
                ctx.PendingRollback = RollbackSignal.None;

                // —— AC 4.3：dot/control 回合间结算 ——
                TickDots(pendingDots, pendingControls, ref hpA, ref hpB);

                // —— 批4 turn-loop：逐回合状态标记消费（goldenBodyTurns/residualOrder 等）——
                TickTurnState(ctx);
            }

            // —— AC 4.6：胜者 HP 高；同时死时累计伤害高者胜；平 Tiebreak(CharacterId) ——
            bool aIsWinner;
            int margin;
            if (hpA > hpB)
            {
                aIsWinner = true;
                margin = hpA - hpB;
            }
            else if (hpB > hpA)
            {
                aIsWinner = false;
                margin = hpB - hpA;
            }
            else if (totalDmgToB > totalDmgToA)
            {
                // 同时死 → a 累计伤害更高 → a 胜；margin = 累计伤害差
                aIsWinner = true;
                margin = (int)Math.Min(int.MaxValue, totalDmgToB - totalDmgToA);
            }
            else if (totalDmgToA > totalDmgToB)
            {
                aIsWinner = false;
                margin = (int)Math.Min(int.MaxValue, totalDmgToA - totalDmgToB);
            }
            else
            {
                // 真平局：Tiebreak(CharacterId)
                aIsWinner = attacker.Id.Value < defender.Id.Value;
                margin = 0;
            }

            return new Result(
                aIsWinner ? attacker.Id : defender.Id,
                aIsWinner ? defender.Id : attacker.Id,
                margin,
                WasAutoWin: false,
                AttackerHpRemaining: hpA,
                DefenderHpRemaining: hpB,
                AttackerStatDeltas: ctx.GetStatDeltas(Side.Attacker),
                DefenderStatDeltas: ctx.GetStatDeltas(Side.Defender),
                AttackerRelationDelta: ctx.GetRelationDelta(Side.Attacker),
                DefenderRelationDelta: ctx.GetRelationDelta(Side.Defender));
        }

        /// <summary>
        /// 单次交锋结算：攻方 OnUse→防方 OnDefend→软情境→(对防方伤害, 反伤回攻方量)。
        /// Dot/Control 模块不直接改 dmg，而是通过 out 列表挂载到对应方，TickDots 结算。
        /// </summary>
        private static (int DmgToDefender, int ReflectToAttacker) ResolveExchange(
            CombatSkillDef? skill,
            int attackerPe,
            CultivationState defenderState,
            CultivationPathDef attackerPath, CultivationPathDef defenderPath,
            CombatContext ctx, LimitsConfig limits, SituationalResolver? resolver,
            Side attackerSide, Side defenderSide,
            List<DotEntry> pendingDots, List<ControlEntry> pendingControls,
            ControlLimiterState controlLimiter, int round,
            ArtifactDef? attackerArtifact = null, ArtifactDef? defenderArtifact = null,
            bool calibrationMode = false)
        {
            const int Scale = 100;
            long dmg = (long)attackerPe * Scale / BaseDamageDivisor;
            long totalReflect = 0;

            // balance-003: 模块伤害增量上限 = PE/4（未缩放空间）。防 PenFromResource 等一击必杀。
            // PE/4 in unscaled = 2.5× base damage. Sufficient edge without one-shot.
            ctx.ModuleDamageCap = attackerPe / 4;

            // OnUse：经 ModuleResolver 逐模块修正
            if (skill != null)
            {
                foreach (var op in skill.OnUse)
                {
                    // Dot/Control: 不修改 dmg，挂载到防方侧，TickDots 结算
                    if (op.Kind == EffectOpKind.Dot)
                    {
                        int turns = op.Amount2 >= 1 ? op.Amount2 : 1;
                        pendingDots.Add(new DotEntry(defenderSide, op.Key ?? "dot", op.Amount, turns));
                        continue;
                    }
                    if (op.Kind == EffectOpKind.Control)
                    {
                        // balance-006 方案B：标定模式旁路 Control（锁回合破坏行动经济，与裸 PE 平价正交）。
                        if (calibrationMode) continue;
                        int baseTurns = op.Amount >= 1 ? op.Amount : 1;
                        string ctrlKey = op.Key ?? "ctrl";
                        var limiterKey = (defenderSide, ctrlKey);

                        // balance-007 硬冷却：上次挂控后 ControlCooldown 回合内该控不可再挂（给博弈窗口）。
                        if (controlLimiter.CooldownUntilRound.TryGetValue(limiterKey, out int until)
                            && round < until)
                            continue;

                        // balance-007 抗性递减（duration-based）：同目标同类控制重复 → 有效回合阶梯降，0=免疫。
                        controlLimiter.HitCount.TryGetValue(limiterKey, out int priorHits);
                        int effTurns = EffectiveControlTurns(baseTurns, priorHits, limits.ControlDRStep);
                        if (effTurns <= 0)
                        {
                            // 免疫：仍记 hit 使 DR 单调推进（重复施加不重置阶梯），但不挂控、不进冷却。
                            controlLimiter.HitCount[limiterKey] = priorHits + 1;
                            continue;
                        }

                        pendingControls.Add(new ControlEntry(defenderSide, ctrlKey, effTurns));
                        controlLimiter.HitCount[limiterKey] = priorHits + 1;
                        controlLimiter.CooldownUntilRound[limiterKey] = round + limits.ControlCooldown;
                        continue;
                    }
                    // balance-006 方案B：标定模式旁路 CounterMul（tag 克制倍乘，与裸 PE 正交）。
                    if (calibrationMode && op.Kind == EffectOpKind.CounterMul) continue;
                    int dmgUnscaled = (int)(dmg / Scale);
                    int result = ModuleResolver.ApplyOnUse(dmgUnscaled, op, ctx);
                    dmg = (long)result * Scale + (dmg % Scale);
                }
            }

            // —— 批4 GoldenBodyMax anti_evil×1.5：攻方金身大成态内对 evil tag 目标伤害放大 ——
            if (HasTurnResource(ctx, attackerSide, "goldenBodyTurns")
                && HasTag(defenderPath, "evil"))
            {
                dmg = dmg * 3 / 2;
            }

            // —— 批5 法宝配套：攻方装备法宝 OnUse 效果 ——
            if (attackerArtifact != null)
            {
                foreach (var op in attackerArtifact.Effects)
                {
                    if (op.Trigger == EffectTrigger.OnDefend) continue; // OnDefend 效果在防方阶段处理
                    if (op.Kind == EffectOpKind.Dot || op.Kind == EffectOpKind.Control) continue; // dot/ctrl 非直伤
                    int dmgUnscaled = (int)(dmg / Scale);
                    int result = ModuleResolver.ApplyOnUse(dmgUnscaled, op, ctx);
                    dmg = (long)result * Scale + (dmg % Scale);
                }
            }

            // —— 批5 法宝配套：防方装备法宝 OnDefend 效果（盾/护甲等） ——
            if (defenderArtifact != null)
            {
                foreach (var op in defenderArtifact.Effects)
                {
                    if (op.Trigger != EffectTrigger.OnDefend) continue;
                    int dmgUnscaled = (int)(dmg / Scale);
                    int result = ModuleResolver.ApplyOnDefend(dmgUnscaled, op, ctx, defenderSide, out int artReflect);
                    dmg = (long)result * Scale + (dmg % Scale);
                    totalReflect += artReflect;
                }
            }

            // OnDefend：防方防御模块（经 ModuleResolver.ApplyOnDefend），收集反伤
            foreach (var defSkill in defenderPath.CombatSkills)
            {
                if (!ListHas(defenderState.ChosenSkillIds, defSkill.Id)) continue;
                if (defSkill.Tier > defenderState.RealmIndex) continue;
                foreach (var op in defSkill.OnUse)
                {
                    if (op.Trigger != EffectTrigger.OnDefend) continue;
                    // Gate check now in ModuleResolver.ApplyOnDefend via op.Gate field
                    int dmgUnscaled = (int)(dmg / Scale);
                    int result = ModuleResolver.ApplyOnDefend(dmgUnscaled, op, ctx, defenderSide, out int reflectDmg);
                    dmg = (long)result * Scale + (dmg % Scale);
                    totalReflect += reflectDmg;
                }
            }

            // 负向压制矩阵检查（PostMul — 在 FlatPen/FlatDR 之后、软情境之前乘算）
            // balance-006 方案B：标定模式旁路压制（阴→阳/魔→佛 结构性克制，与裸 PE 平价正交）。
            if (!calibrationMode)
            {
                int suppressionRatio = SuppressionMatrix.GetSuppressionRatio(
                    attackerPath.SituationalTags, defenderPath?.SituationalTags ?? Array.Empty<string>());
                if (suppressionRatio != 10)
                    dmg = dmg * suppressionRatio / 10;
            }

            // —— 批4 GoldenBodyMax DR×2 + 受击转愿：防方金身大成态内伤害减半，吸收部分转愿力 ——
            if (HasTurnResource(ctx, defenderSide, "goldenBodyTurns"))
            {
                long dmgBeforeDR = dmg;
                dmg = dmg / 2; // DR×2（伤害减半）
                long dmgAbsorbed = dmgBeforeDR - dmg; // 金身吸收的伤害量
                if (ctx.HasResource(defenderSide, "vow"))
                    ctx.ApplyResource(defenderSide, "vow", (int)dmgAbsorbed);
            }

            // 软情境修正
            if (resolver != null)
            {
                var defTags = defenderPath?.SituationalTags ?? Array.Empty<string>();
                var sitCtx = new SitContext(
                    attackerPath.SituationalTags, defTags,
                    attackerPath.AttackDimension,
                    new Dictionary<string, string>());
                int adj = resolver.AdjPct(sitCtx, limits.SituationalP0Base);
                dmg = dmg * (100 + adj) / 100;
            }

            return ((int)Math.Max(0, dmg / Scale), (int)totalReflect);
        }

        /// <summary>dot 挂载条目：对哪方、每 tick 伤害、剩余回合。</summary>
        private sealed class DotEntry
        {
            public readonly Side Target;
            public readonly string Key;
            public readonly int PerTick;
            public int TurnsRemaining;
            public DotEntry(Side target, string key, int perTick, int turns)
            { Target = target; Key = key; PerTick = perTick; TurnsRemaining = turns; }
        }

        /// <summary>control 挂载条目：对哪方、剩余回合（>0 则该方本回合伤害=0）。</summary>
        private sealed class ControlEntry
        {
            public readonly Side Target;
            public readonly string Key;
            public int TurnsRemaining;
            public ControlEntry(Side target, string key, int turns)
            { Target = target; Key = key; TurnsRemaining = turns; }
        }

        /// <summary>
        /// balance-007：控制冷却/递减 duel-local 状态（每场对拍内，不入 World.Clone/Character 持久态
        /// → B.3 off 逐字节天然安全，off 更不调 DuelEngine）。仅按 (Side,key) 键读写，从不遍历 → 无序不影响确定性（B.2）。
        /// </summary>
        private sealed class ControlLimiterState
        {
            /// <summary>(目标,key) → 该控制下次可再挂的回合号（round < 此值则本回合拒挂）。</summary>
            public readonly Dictionary<(Side, string), int> CooldownUntilRound = new Dictionary<(Side, string), int>();
            /// <summary>(目标,key) → 该控制已成功施加次数（供 DR 阶梯递减）。</summary>
            public readonly Dictionary<(Side, string), int> HitCount = new Dictionary<(Side, string), int>();
        }

        /// <summary>
        /// balance-007 抗性递减（duration-based，纯整数 B.2）：同目标同类控制重复施加，
        /// 有效持续回合数 = max(0, baseTurns − priorHits × drStep)。0 = 免疫（不挂控）。
        /// 纯函数（无副作用），供 ResolveExchange 调用 + 单测直接验（对拍无 RNG，确定）。
        /// </summary>
        /// <param name="baseTurns">控制基础持续回合（Modules.Control 的 turns）</param>
        /// <param name="priorHits">此前已对该目标施加同类控制的次数</param>
        /// <param name="drStep">每次重复的递减步长（LimitsConfig.ControlDRStep；0=无递减）</param>
        public static int EffectiveControlTurns(int baseTurns, int priorHits, int drStep)
            => Math.Max(0, baseTurns - priorHits * drStep);

        /// <summary>回合间 dot/control 结算（AC 4.3）。dot 扣 hp；control 减回合。</summary>
        private static void TickDots(List<DotEntry> dots, List<ControlEntry> controls, ref int hpA, ref int hpB)
        {
            // Dot：每 tick 扣对应方 HP
            for (int i = dots.Count - 1; i >= 0; i--)
            {
                var d = dots[i];
                if (d.Target == Side.Attacker)
                    hpA = Math.Max(0, hpA - d.PerTick);
                else
                    hpB = Math.Max(0, hpB - d.PerTick);
                d.TurnsRemaining--;
                if (d.TurnsRemaining <= 0)
                    dots.RemoveAt(i);
            }
            // Control：只减回合（伤害归零由被控方 dmg=0 实现）
            for (int i = controls.Count - 1; i >= 0; i--)
            {
                var c = controls[i];
                c.TurnsRemaining--;
                if (c.TurnsRemaining <= 0)
                    controls.RemoveAt(i);
            }
        }

        // ================================================================
        // 批4 turn-loop：逐回合状态标记消费
        // ================================================================

        /// <summary>GuRevolt 阵营反噬阈值（≥ 此值触发行动重定向）。</summary>
        public const int GuRevoltRedirectThreshold = 50;

        /// <summary>GuRevolt 每回合自然衰减量（触发反噬后额外扣减）。</summary>
        public const int GuRevoltDecayPerTurn = 20;

        /// <summary>BrokenChain residualOrder 每回合衰减量。</summary>
        public const int ResidualOrderDecayPerTurn = 25;

        /// <summary>
        /// 逐回合状态标记消费（批4 turn-loop）：递减回合标记资源，到期触发回调。
        /// 接续 TickDots 之后调用，处理 goldenBodyTurns/residualOrder/guRevolt 等逐回合标记。
        /// </summary>
        private static void TickTurnState(CombatContext ctx)
        {
            // —— goldenBodyTurns（佛修金身大成态）：每回合-1，到期回退 goldenLayers+2 ——
            DecayTurnResource(ctx, Side.Attacker, "goldenBodyTurns",
                onExpire: () => ctx.ApplyResource(Side.Attacker, "goldenLayers", -2));
            DecayTurnResource(ctx, Side.Defender, "goldenBodyTurns",
                onExpire: () => ctx.ApplyResource(Side.Defender, "goldenLayers", -2));

            // —— residualOrder（傀儡师断链残命惯性）：每回合递减，到期军团僵死 ——
            DecayTurnResource(ctx, Side.Attacker, "residualOrder", ResidualOrderDecayPerTurn,
                onExpire: null); // fleet freeze 由 HasResidualOrder 检查
            DecayTurnResource(ctx, Side.Defender, "residualOrder", ResidualOrderDecayPerTurn,
                onExpire: null);

            // —— guRevolt（毒蛊植蛊反噬度）：触发反噬后额外衰减 ——
            DecayTurnResource(ctx, Side.Attacker, "guRevolt", GuRevoltDecayPerTurn, onExpire: null);
            DecayTurnResource(ctx, Side.Defender, "guRevolt", GuRevoltDecayPerTurn, onExpire: null);
        }

        /// <summary>
        /// 递减一方的一个回合标记资源（amount > 0 才减）。减后=0 触发 onExpire 回调。
        /// </summary>
        /// <param name="decay">每回合递减值（默认 1）</param>
        private static void DecayTurnResource(CombatContext ctx, Side side, string key,
            int decay = 1, Action? onExpire = null)
        {
            if (!ctx.HasResource(side, key)) return;
            int current = ctx.ReadResource(side, key);
            if (current <= 0) return;
            int newVal = Math.Max(0, current - decay);
            ctx.ApplyResource(side, key, -(current - newVal)); // negative delta to reach newVal
            if (newVal == 0 && current > 0)
                onExpire?.Invoke();
        }

        /// <summary>检查一方是否有 >0 的指定回合标记资源。</summary>
        private static bool HasTurnResource(CombatContext ctx, Side side, string key)
        {
            if (!ctx.HasResource(side, key)) return false;
            return ctx.ReadResource(side, key) > 0;
        }

        /// <summary>检查一方是否有 residualOrder > 0（傀儡师仍在残命惯性内）。</summary>
        private static bool HasResidualOrder(CombatContext ctx, Side side)
            => HasTurnResource(ctx, side, "residualOrder");

        /// <summary>检查一方 guRevolt 是否达到阵营反噬阈值。</summary>
        private static bool IsGuRevoltRedirected(CombatContext ctx, Side side)
        {
            if (!ctx.HasResource(side, "guRevolt")) return false;
            return ctx.ReadResource(side, "guRevolt") >= GuRevoltRedirectThreshold;
        }

        /// <summary>检查某方是否被控（control turns>0）。被控则攻击伤害=0。</summary>
        private static bool IsControlled(List<ControlEntry> controls, Side side)
        {
            foreach (var c in controls)
                if (c.Target == side && c.TurnsRemaining > 0)
                    return true;
            return false;
        }

        /// <summary>
        /// 自动选最优战技：从角色已学技能中选 tier≤realm 且 TryPayCost 通过的最高 tier 技能。
        /// 无可选技能时返回 null（裸攻）。
        /// </summary>
        private static CombatSkillDef? SelectBestSkill(CultivationState st, CultivationPathDef path)
        {
            CombatSkillDef? best = null;
            int bestTier = -1;
            foreach (var skillId in st.ChosenSkillIds)
            {
                foreach (var skill in path.CombatSkills)
                {
                    if (skill.Id != skillId) continue;
                    if (skill.Tier > st.RealmIndex) continue; // tier gate
                    if (skill.Cost.Count > 0 && !EffectInterpreter.TryPayCost(skill.Cost, st))
                        continue;
                    if (skill.Tier > bestTier)
                    {
                        bestTier = skill.Tier;
                        best = skill;
                    }
                }
            }
            return best;
        }

        /// <summary>Validate skill: tier≤realm & TryPayCost & artifact gate; returns null if invalid.</summary>
        private static CombatSkillDef? ValidateSkill(CombatSkillDef? skill, CultivationState st, CultivationPathDef path)
        {
            if (skill == null) return null;
            if (skill.Tier > st.RealmIndex) return null;
            if (skill.Cost.Count > 0 && !EffectInterpreter.TryPayCost(skill.Cost, st))
                return null;
            // Artifact gate (AC 5.2): artifact-related skills require artifact arts
            if (IsArtifactSkill(skill) && !HasArtifactArt(path, st))
                return null;
            return skill;
        }

        /// <summary>Check if skill uses artifact-related modules (itemTier resource or luobao special).</summary>
        private static bool IsArtifactSkill(CombatSkillDef skill)
        {
            foreach (var op in skill.OnUse)
            {
                if (op.Key == "itemTier") return true;
                if (op.Kind == EffectOpKind.Special && op.Key == "luobao") return true;
            }
            return false;
        }

        /// <summary>检查防方是否修了轻功/身法类功法（门控闪避）。</summary>
        private static bool HasMovementArt(CultivationPathDef path, CultivationState st)
        {
            foreach (var cat in path.ArtCategories)
            {
                if (cat.Role == "movement")
                {
                    foreach (var art in cat.Arts)
                        if (ListHas(st.ChosenArtIds, art.Id))
                            return true;
                }
            }
            return false;
        }

        /// <summary>检查是否修了御器/法宝类功法（门控法宝技能）。</summary>
        private static bool HasArtifactArt(CultivationPathDef path, CultivationState st)
        {
            foreach (var cat in path.ArtCategories)
            {
                if (cat.Role == "core_forge" || cat.Role == "channel_mind" || cat.Role == "named_artifacts")
                {
                    foreach (var art in cat.Arts)
                        if (ListHas(st.ChosenArtIds, art.Id))
                            return true;
                }
            }
            return false;
        }

        /// <summary>检查防方是否修了横练/护体类功法（门控反伤/格挡）。</summary>
        private static bool HasBodyArt(CultivationPathDef path, CultivationState st)
        {
            foreach (var cat in path.ArtCategories)
            {
                if (cat.Role == "body" || cat.Role == "defense")
                {
                    foreach (var art in cat.Arts)
                        if (ListHas(st.ChosenArtIds, art.Id))
                            return true;
                }
            }
            return false;
        }

        private static int UnifiedTier(CultivationState st, CultivationPathDef path)
        {
            if (st.RealmIndex < path.Curve.UnifiedTierOf.Count)
                return path.Curve.UnifiedTierOf[st.RealmIndex];
            return path.Curve.UnifiedTierOf[path.Curve.UnifiedTierOf.Count - 1];
        }

        private static bool ListHas(IReadOnlyList<string> list, string item)
        {
            foreach (var x in list)
                if (x == item) return true;
            return false;
        }

        /// <summary>检查路径是否含指定 SituationalTag。</summary>
        private static bool HasTag(CultivationPathDef path, string tag)
        {
            foreach (var t in path.SituationalTags)
                if (t == tag) return true;
            return false;
        }

        /// <summary>
        /// 夺舍 gate 检查（story fullstruct-007）：对手路径含 thunder/pure_yang/buddha_light 标签
        /// → 夺舍被压制（return true=blocked）。雷电/纯阳/佛光克制阴邪夺舍。
        /// </summary>
        private static bool CheckPossessionGate(CultivationPathDef opponentPath)
        {
            foreach (var tag in opponentPath.SituationalTags)
            {
                if (tag == "thunder" || tag == "pure_yang" || tag == "buddha_light")
                    return true; // gate blocked
            }
            return false;
        }
    }
}
