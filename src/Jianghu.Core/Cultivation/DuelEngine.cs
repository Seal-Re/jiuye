using System;
using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Cultivation.Artifacts;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Stats;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// R2 战斗结算引擎（story-003 batch4 + fullstruct-007 rollback stack）。接模块系统：HP=pe、选招经 tier/cost/gate、
    /// OnUse→OnDefend→软情境同时扣血。回合间 push RollbackStack 快照，支持因果逆演/夺舍续命的
    /// 结算回滚。纯整数确定性（同种子逐字节复现）；cv-001（adr-0008）起引入 duelRng 做 Margin→概率伯努利判定，duel-local 不入 World.Clone（off 不构造→B.3 天然守）。
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
            bool calibrationMode = false,
            IRandom? duelRng = null)
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
            var poise = new PoiseState(limits.PoiseMax);     // cv-002：削韧副轴 duel-local（不入 Clone → B.3 天然守）
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
                var (rawDmgToB, reflectToA, poiseBonusToB, chipImmuneB) = ResolveExchange(
                    activeASkill, effPeA, defender.Cultivation, defender.Stats,
                    attackerPath, defenderPath, ctx, limits, resolver,
                    Side.Attacker, Side.Defender,
                    pendingDots, pendingControls, controlLimiter, round,
                    attackerArtifact, defenderArtifact, calibrationMode,
                    defenderPe: effPeB, duelRng: duelRng, exchangeNonce: 0);

                int dmgToB, dmgToA_redirect;
                if (attackerControlled || aFleetFrozen)
                { dmgToB = 0; dmgToA_redirect = 0; }
                else if (aRedirected)
                { dmgToB = 0; dmgToA_redirect = rawDmgToB; } // 反噬：自伤
                else
                { dmgToB = rawDmgToB; dmgToA_redirect = 0; }

                // cv-002：削韧 bonus 仅在招式"正常命中防方"时施加（被控/僵死/反噬→招式未落对方，无 bonus）。
                bool aHitB = !attackerControlled && !aFleetFrozen && !aRedirected;

                // —— Exchange 2: 防方→攻方 ——
                var (rawDmgToA, reflectToB, poiseBonusToA, chipImmuneA) = ResolveExchange(
                    activeDSkill, effPeB, attacker.Cultivation, attacker.Stats,
                    defenderPath, attackerPath, ctx, limits, resolver,
                    Side.Attacker, Side.Defender,
                    pendingDots, pendingControls, controlLimiter, round,
                    defenderArtifact, attackerArtifact, calibrationMode,
                    defenderPe: effPeA, duelRng: duelRng, exchangeNonce: 1);

                int dmgToA, dmgToB_redirect;
                if (defenderControlled || bFleetFrozen)
                { dmgToA = 0; dmgToB_redirect = 0; }
                else if (bRedirected)
                { dmgToA = 0; dmgToB_redirect = rawDmgToA; } // 反噬：自伤
                else
                { dmgToA = rawDmgToA; dmgToB_redirect = 0; }

                // cv-002：防方招式正常命中攻方？
                bool bHitA = !defenderControlled && !bFleetFrozen && !bRedirected;

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

                // —— cv-002（adr-0008 决策⑦步7）：削韧副轴结算（TickDots 之后！）——
                // 时序铁律：stagger 注入必须在 TickDots 之后，否则本回合末 TickDots 会把 turns=1 立即递减到 0
                // → 下回合 IsControlled=false → 哑弹（balance-008 教训）。此处注入的裸 turns=1 不经 StoredControlTurns
                // 补偿（补偿是给"回合早期 ResolveExchange 挂载、会历经本回合 TickDots"的算子控制用；此注入点已在
                // TickDots 之后，天然拒止下回合 1 次 = 打断语义）。削韧值 = derive(实际伤害) + 命中时的算子 bonus。
                // balance-006 方案B：标定模式旁路削韧（stagger 锁回合破坏行动经济，与裸 PE 平价正交，
                // 同 Control/CounterMul/压制旁路一致）。cv-005 若带方差重标定再评估纳入。
                // cv-003（决策⑩.1）：Chip 穿透交锋免控制/硬直 → chipImmune 时该方向削韧（基础+bonus）全归零。
                if (!calibrationMode)
                {
                    int poiseToB = chipImmuneB ? 0 : DerivePoiseDamage(dmgToB + reflectToB, limits.PoiseDamageRatioPermille) + (aHitB ? poiseBonusToB : 0);
                    int poiseToA = chipImmuneA ? 0 : DerivePoiseDamage(dmgToA + reflectToA, limits.PoiseDamageRatioPermille) + (bHitA ? poiseBonusToA : 0);
                    TickPoiseDirect(poise, pendingControls, round, limits, poiseToB, poiseToA);
                }

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
        /// 单次交锋结算：攻方 OnUse→防方 OnDefend→软情境→(对防方伤害, 反伤回攻方量, 削韧 bonus, Chip 免削韧)。
        /// Dot/Control 模块不直接改 dmg，而是通过 out 列表挂载到对应方，TickDots 结算。
        /// cv-003：ChipImmuneToPoise=true 时该交锋伤害为 Elemental 格挡穿透（免控制/硬直，⑩.1）→ ResolveR2 不派生削韧。
        /// </summary>
        private static (int DmgToDefender, int ReflectToAttacker, int PoiseBreakBonus, bool ChipImmuneToPoise) ResolveExchange(
            CombatSkillDef? skill,
            int attackerPe,
            CultivationState defenderState, StatBlock defenderStats,
            CultivationPathDef attackerPath, CultivationPathDef defenderPath,
            CombatContext ctx, LimitsConfig limits, SituationalResolver? resolver,
            Side attackerSide, Side defenderSide,
            List<DotEntry> pendingDots, List<ControlEntry> pendingControls,
            ControlLimiterState controlLimiter, int round,
            ArtifactDef? attackerArtifact = null, ArtifactDef? defenderArtifact = null,
            bool calibrationMode = false,
            int defenderPe = 0, IRandom? duelRng = null, int exchangeNonce = 0)
        {
            // —— cv-001（adr-0008 决策①③④）：主动交锋概率拦截（管线1）——
            // duelRng!=null（仅生产 sim，off/单测传 null→确定性旁路，既有断言逐字节不变）时，
            // 查 CombatMath 表 → 伯努利判定。未命中 = 攻击被化解（本切片伤害归零 + 不施本次 rider dot/control：
            // "弹反挂毒的暗器→无毒"，用户界定）；已挂 DoT 仍由 TickDots 结算（管线2 绕过，不受影响）。
            // 防守帧钩子/削韧/标签门控/溢出留 cv-002~004。标定期（calibrationMode）旁路方差（只测裸 PE）。
            if (duelRng != null && !calibrationMode && skill != null)
            {
                int p = CombatMath.GetSuccessPermille(attackerPe - defenderPe, defenderPe);
                // cv-006（adr-0010 决策①）：SEC 前置调制——合流进 cv-001 单次伯努利（不新增掷骰）。
                // 已在 !calibrationMode 块内 → 标定模式天然旁路 SEC（AC 6.5，保 cv-005 seed-sweep 裸 PE 纯净）。
                // skill!=null 已由外层守卫 → 直接 skill.Sec（裸攻走不到此分支，AC 6.3 裸攻默认中性由外层跳过本块实现）。
                p = CombatMath.ApplyEvasionCoefficient(p, skill.Sec);
                // cv-004（adr-0008 决策⑨.2）：阈值溢出检测——p≥OverflowThreshold 时跳过伯努利掷骰（数学必中）。
                // SEC=0 必中(Apply 返 AutoHitPermille=1000) 已达阈值，走溢出路径 → 语义合理（必中标签 = 威压溢出）。
                // 后续 cv-004-b 扩展：溢出时跳过 OnDefend（绝对秒杀）+ View 侧防守帧钩子。
                bool overflowed = CombatMath.IsOverflow(p, limits.OverflowThresholdPermille);
                if (!overflowed)
                {
                    int roll = duelRng.Split((ulong)((round << 4) | exchangeNonce)).NextInt(1000);
                    if (roll >= p)
                        return (0, 0, 0, false); // 未命中：攻击化解，本次交锋零伤害、零 rider 挂载、零削韧
                }
                // 溢出/命中：继续执行后续伤害计算（OnUse → OnDefend → ...）
            }

            // —— cv-002（adr-0008 决策⑦步7）：PoiseBreak 算子额外削韧收集（路线 B）——
            // 强控标签附带的显式削韧算子，累加基础派生削韧之外的 bonus。不产直伤（continue）。
            // 基础削韧（从有效伤害派生）在 ResolveR2 的 TickPoise 里算，此处仅收集算子附加量。
            int poiseBreakBonus = 0;
            if (skill != null)
            {
                foreach (var op in skill.OnUse)
                {
                    if (op.Kind == EffectOpKind.PoiseDamage)
                        poiseBreakBonus += op.Amount >= 0 ? op.Amount : 0; // 削韧量非负
                }
            }

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

                        pendingControls.Add(new ControlEntry(defenderSide, ctrlKey, StoredControlTurns(baseTurns, effTurns)));
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

            // —— cv-003（adr-0008 决策⑨.1/⑩.1）：标签门控 + Chip 穿透准备 ——
            // 攻击 DamageType（攻方招式级；裸攻/无招默认 Normal）。标定模式旁路门控（同 Control/压制，保裸 PE 平价）。
            DamageType attackType = (!calibrationMode && skill != null) ? skill.Damage : DamageType.Normal;
            bool blockFired = false;          // 本次交锋是否有 Block 类防御实际减伤（供 Elemental Chip 判定）
            bool blockGatedByBlunt = false;   // Blunt 是否门控关掉了防方 Block 类（供招架崩坏削韧 bonus）

            // —— 批5 法宝配套：防方装备法宝 OnDefend 效果（盾/护甲等） ——
            if (defenderArtifact != null)
            {
                foreach (var op in defenderArtifact.Effects)
                {
                    if (op.Trigger != EffectTrigger.OnDefend) continue;
                    // cv-003 门控：Blunt 关 Block 类 / Elemental 关 Dodge 类 → 该防御失效（continue）。
                    if (IsDefenseGatedOut(attackType, op.Kind))
                    {
                        if (IsBlockClass(op.Kind)) blockGatedByBlunt = true;
                        continue;
                    }
                    long dmgBefore = dmg;
                    int dmgUnscaled = (int)(dmg / Scale);
                    int result = ModuleResolver.ApplyOnDefend(dmgUnscaled, op, ctx, defenderSide, out int artReflect);
                    dmg = (long)result * Scale + (dmg % Scale);
                    totalReflect += artReflect;
                    if (IsBlockClass(op.Kind) && dmg < dmgBefore) blockFired = true;
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
                    // cv-003 门控：Blunt 关 Block 类 / Elemental 关 Dodge 类 → 该防御失效（continue）。
                    if (IsDefenseGatedOut(attackType, op.Kind))
                    {
                        if (IsBlockClass(op.Kind)) blockGatedByBlunt = true;
                        continue;
                    }
                    // Gate check now in ModuleResolver.ApplyOnDefend via op.Gate field
                    long dmgBefore = dmg;
                    int dmgUnscaled = (int)(dmg / Scale);
                    int result = ModuleResolver.ApplyOnDefend(dmgUnscaled, op, ctx, defenderSide, out int reflectDmg);
                    dmg = (long)result * Scale + (dmg % Scale);
                    totalReflect += reflectDmg;
                    if (IsBlockClass(op.Kind) && dmg < dmgBefore) blockFired = true;
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

            // —— cv-003（adr-0008 决策⑩.1）：元素格挡穿透 Chip Damage ——
            // Elemental 攻击被 Block 类成功减伤 → 免控制/硬直（chipImmune）但承受穿透保底。
            // 基础伤害 = attackerPe/BaseDamageDivisor（未缩放）；Margin = attackerPe − defenderPe。
            // cv-008（adr-0010 决策②）：SBC 招式格挡系数调制有效 Chip 穿透千分比（替换 limits.ChipDamagePermille）。
            // 复用 cv-003 保底模型 + 既有 ChipDamageFloor 函数（不引入新公式、不内联）。SBC 确定性不掷骰。
            // 标定/裸攻（calibrationMode || skill==null）用基准 ChipPermille（同 cv-001/002/003/006/007 旁路范式）。
            bool chipImmuneToPoise = false;
            if (attackType == DamageType.Elemental && blockFired)
            {
                int effChipPermille = (!calibrationMode && skill != null)
                    ? CombatMath.ApplyBlockCoefficient(limits.ChipDamagePermille, skill.Sbc)
                    : limits.ChipDamagePermille;  // 标定/裸攻用基准
                int chipFloor = ChipDamageFloor(
                    attackerPe / BaseDamageDivisor, attackerPe - defenderPe,
                    effChipPermille, limits.ChipMarginDivisor);
                int dmgUnscaledNow = (int)Math.Max(0, dmg / Scale);
                if (chipFloor > dmgUnscaledNow)
                {
                    dmg = (long)chipFloor * Scale;      // 穿透保底：减伤后仍不低于 chip
                    chipImmuneToPoise = true;           // 穿透伤害免削韧（保霸体，⑩.1）
                }
            }

            // —— cv-007（adr-0010 决策③④ step 4）：第三层抵抗——派生抗性 R 半衰减伤 ——
            // 位置铁律（用户 2026-07-14 裁定修正）：OnDefend 模块结算 + SuppressionMatrix + GoldenBody + 软情境 + Chip
            // **全部之后**、cv-002 削韧派生（在 ResolveR2 TickPoise 内，本函数 return 后）之前。
            // adr-0010 决策④ step 4：抵抗层是漏斗最后一环，对经 Chip 穿透保底后的最终 dmg 做半衰减。
            // R = ResistanceProviders.ResistanceOf（从体质/识/HasBodyArt 功法标签派生，B.5 不含 daoHeart/innerDemon）。
            // 半衰：dmg = CombatMath.ApplyResistance(dmg, R, K)（K×1000/(K+R)，max(1,...) 保底，纯整数 B.2，long 中间防溢出）。
            // 标定模式旁路（同 cv-001/002/003/006，保 cv-005 seed-sweep 裸 PE 纯净）。
            // attackType 取攻方招式 DamageType（标定模式已退化为 Normal，故标定模式本块不进——双保险）。
            if (!calibrationMode)
            {
                int R = ResistanceProviders.ResistanceOf(
                    defenderState, defenderStats, defenderPath!, GateType.None,
                    attackType, limits);
                int dmgUnscaledResist = (int)(dmg / Scale);
                int afterResist = CombatMath.ApplyResistance(dmgUnscaledResist, R, limits.ResistanceHalfLifeK);
                dmg = (long)afterResist * Scale + (dmg % Scale);
            }

            // —— cv-003（adr-0008 决策⑨.1）：招架崩坏 —— Blunt 门控关掉了防方 Block 类 → 追加大额削韧。
            // NPC 侧退化：Block 禁用（全额伤害上面已实现）+ 削韧 bonus（复用 cv-002 PoiseBreakBonus 通道）。
            if (attackType == DamageType.Blunt && blockGatedByBlunt)
                poiseBreakBonus += limits.GuardBreakPoiseBonus;

            return ((int)Math.Max(0, dmg / Scale), (int)totalReflect, poiseBreakBonus, chipImmuneToPoise);
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

        /// <summary>
        /// balance-008 分级语义（方案C，2026-07-06 用户裁定）：turns=1 = 即时打断（interrupt），
        /// turns≥2 = 定身/长控（stun，保持 balance-007 的 N−1 拒止不变）。纯整数（B.2），纯函数。
        ///
        /// tick 时序令控制在"施加回合末"白耗一次递减 → turns=N 实际拒止 N−1 回合。故 baseTurns=1
        /// 现为哑弹（拒止 0）。本函数对 baseTurns=1 存 effTurns+1 补偿该损耗（→ 拒止 1 回合=打断）；
        /// baseTurns≥2 存 effTurns 原样（拒止 N−1 不变，balance-007 CD/DR 逐字节守）。
        /// 前置：仅在 effTurns>0（未被 DR 免疫）时调用；DR 递减后的 effTurns 不触发补偿（守 balance-007）。
        /// </summary>
        /// <param name="baseTurns">控制基础持续回合（Modules.Control 的 turns，判 interrupt vs stun 档）</param>
        /// <param name="effTurns">经 DR 递减后的有效回合（EffectiveControlTurns 输出，>0）</param>
        public static int StoredControlTurns(int baseTurns, int effTurns)
            => baseTurns == 1 ? effTurns + 1 : effTurns;

        /// <summary>
        /// cv-003（adr-0008 决策⑨.1）：判 OnDefend 算子是否 **Block 类**（招架/护体/减伤/反震）。
        /// Blunt（Unblockable_Weapon）攻击门控关闭此类。纯函数（B.2），供 ResolveExchange + 单测直验。
        /// Block 类 = {AddFlatDR 固定减伤, ReflectDamage 反震}（+ 法宝盾另判）。
        /// 注（cv-003 非对称，主控 code-review 2026-07-07 记）：ReflectDamage 归 Block 类**仅在 Blunt 门控路径生效**
        /// （Blunt 关反震 + 招架崩坏）；它在 Chip 路径**永不触发 blockFired**——ReflectDamage 不减来袭伤（ModuleResolver
        /// 返回 incoming 原值），故 Elemental 对"仅反震"防方无 chip（元素吃满额本已 ≥ chip 保底，语义自洽）。
        /// </summary>
        public static bool IsBlockClass(EffectOpKind kind)
            => kind == EffectOpKind.AddFlatDR || kind == EffectOpKind.ReflectDamage;

        /// <summary>
        /// cv-003（adr-0008 决策⑨.1）：判 OnDefend 算子是否 **Dodge 类**（闪避/身法规避）。
        /// Elemental（Undodgeable_Space）攻击门控关闭此类。纯函数（B.2）。
        /// Dodge 类 = {Evade 闪避, SoulSplit 分魂挡刀}（用户 2026-07-07 裁定 SoulSplit 归 Dodge——身法秘术，门控同 Evade）。
        /// </summary>
        public static bool IsDodgeClass(EffectOpKind kind)
            => kind == EffectOpKind.Evade || kind == EffectOpKind.SoulSplit;

        /// <summary>
        /// cv-003（adr-0008 决策⑨.1）：攻击 DamageType 是否门控关闭该 OnDefend 防御算子。
        /// Blunt → 关 Block 类；Elemental → 关 Dodge 类；Normal → 全开。纯函数（B.2）。
        /// </summary>
        public static bool IsDefenseGatedOut(DamageType attackType, EffectOpKind defenseKind)
            => (attackType == DamageType.Blunt && IsBlockClass(defenseKind))
            || (attackType == DamageType.Elemental && IsDodgeClass(defenseKind));

        /// <summary>
        /// cv-003（adr-0008 决策⑩.1）：元素格挡穿透 Chip Damage 保底（纯整数 B.2，纯函数）。
        /// Elemental 攻击被 Block 类成功减伤时：免控制/硬直但承受穿透 =
        /// 基础伤害 × chipPermille/1000 + Margin 修正（Margin=攻防 PE 差，divisor 调修正幅度）。
        /// 返回穿透保底值（调用方取 max(dmg, chipFloor)）。chipPermille=0 → 退化无 chip（返 0）。
        /// </summary>
        /// <param name="baseDamage">基础伤害（未缩放，= attackerPe/BaseDamageDivisor）</param>
        /// <param name="peMargin">攻防 PE 差（attackerPe − defenderPe，正=攻方占优 → 穿透更狠）</param>
        /// <param name="chipPermille">穿透基础千分比（LimitsConfig.ChipDamagePermille）</param>
        /// <param name="marginDivisor">Margin 修正除数（LimitsConfig.ChipMarginDivisor；≤0 则无 margin 修正）</param>
        public static int ChipDamageFloor(int baseDamage, int peMargin, int chipPermille, int marginDivisor)
        {
            if (chipPermille <= 0 || baseDamage <= 0) return 0;
            long chip = (long)baseDamage * chipPermille / 1000;
            if (marginDivisor > 0)
                chip += (long)peMargin / marginDivisor; // 正 margin 抬穿透，负 margin 削（向下取整）
            if (chip < 0) return 0;                       // 负 margin 削穿透到负 → 钳 0
            return chip > int.MaxValue ? int.MaxValue : (int)chip; // 防 int 回绕（病态大输入）
        }

        /// <summary>
        /// cv-002：削韧副轴 duel-local 状态（比照 <see cref="ControlLimiterState"/>；每场对拍内，不入
        /// World.Clone/Character 持久态 → B.3 off 逐字节天然安全，off 更不调 DuelEngine）。
        /// 仅按 Side 键读写，从不遍历 → 无序不影响确定性（B.2）。
        /// </summary>
        private sealed class PoiseState
        {
            /// <summary>Side → 当前剩余韧性（初值 PoiseMax；≤0 触发硬直后重置）。</summary>
            public readonly Dictionary<Side, int> Remaining = new Dictionary<Side, int>();
            /// <summary>Side → 该方已被硬直次数（供 DR 阶梯 + 重置量抬升）。</summary>
            public readonly Dictionary<Side, int> StaggerCount = new Dictionary<Side, int>();
            /// <summary>Side → 该方下次可再被硬直的回合号（round < 此值则本回合免疫硬直，防 stagger-lock）。</summary>
            public readonly Dictionary<Side, int> StaggerCooldownUntilRound = new Dictionary<Side, int>();
            private readonly int _poiseMax;
            public PoiseState(int poiseMax)
            {
                _poiseMax = poiseMax < 1 ? 1 : poiseMax;
                Remaining[Side.Attacker] = _poiseMax;
                Remaining[Side.Defender] = _poiseMax;
            }
            public int PoiseMax => _poiseMax;
        }

        /// <summary>
        /// cv-002 基础削韧派生（纯整数 B.2，纯函数）：有效伤害 → 削韧值 = dmg × ratioPermille / 1000（向下取整）。
        /// dmg≤0（未命中/被控）→ 削韧 0（语义自洽：没挨打不削韧）。ratioPermille=0 → 退化无基础削韧。
        /// 供 TickPoise 调用 + 单测直验。
        /// </summary>
        /// <param name="dmg">本次实际落到该方的伤害（含反伤）</param>
        /// <param name="ratioPermille">伤害→削韧千分比（LimitsConfig.PoiseDamageRatioPermille）</param>
        public static int DerivePoiseDamage(int dmg, int ratioPermille)
        {
            if (dmg <= 0 || ratioPermille <= 0) return 0;
            return (int)((long)dmg * ratioPermille / 1000);
        }

        /// <summary>
        /// cv-002 硬直后韧性重置量（纯整数 B.2，纯函数）：随该方已被硬直次数阶梯抬升韧性上限
        /// = poiseMax + priorStaggers × drStep。语义 = "越被打断越难再被打断"（抗性递减防 stagger-lock，
        /// 复用 balance-007 DR 精神）。drStep=0 → 退化恒定重置 poiseMax。
        /// </summary>
        /// <param name="poiseMax">韧性基准上限（LimitsConfig.PoiseMax）</param>
        /// <param name="priorStaggers">此前该方已被硬直次数</param>
        /// <param name="drStep">每次硬直的抗性抬升步长（LimitsConfig.StaggerDRStep；0=无 DR）</param>
        public static int StaggerResetPoise(int poiseMax, int priorStaggers, int drStep)
            => poiseMax + priorStaggers * (drStep < 0 ? 0 : drStep);

        /// <summary>
        /// cv-002 削韧副轴回合结算（adr-0008 决策⑦步7）。**必须在 TickDots 之后调用**（见调用点时序注释）。
        /// 入参 = 各方本回合**已算好**的削韧值（基础派生 + bonus，cv-003 chip-immune 已在调用点归零）。
        /// 韧性 ≤0 且不在硬直冷却内 → 触发硬直：向 pendingControls 注入 turns=1 stagger（复用 Control 管线
        /// → 下回合 IsControlled → dmg=0 = 打断），韧性重置（经 StaggerResetPoise 抗性递减），记 hit + 冷却。
        /// 霸体：韧性 &gt;0 时不注入 stagger（伤害照受不打断）。纯整数，duel-local，无 RNG（确定性 B.2）。
        /// </summary>
        private static void TickPoiseDirect(
            PoiseState poise, List<ControlEntry> pendingControls, int round, LimitsConfig limits,
            int poiseToDefenderB, int poiseToAttackerA)
        {
            ApplyPoiseToSide(poise, pendingControls, round, limits, Side.Defender, poiseToDefenderB);
            ApplyPoiseToSide(poise, pendingControls, round, limits, Side.Attacker, poiseToAttackerA);
        }

        /// <summary>cv-002：对单方施加削韧 + 韧性≤0 触发硬直（复用 Control 管线注入 turns=1 stagger）。</summary>
        private static void ApplyPoiseToSide(
            PoiseState poise, List<ControlEntry> pendingControls, int round, LimitsConfig limits,
            Side side, int poiseDamage)
        {
            if (poiseDamage <= 0) return; // 无削韧（未命中/退化）→ 韧性不变
            poise.Remaining.TryGetValue(side, out int cur);
            cur -= poiseDamage;
            if (cur > 0)
            {
                poise.Remaining[side] = cur; // 霸体：韧性未破，伤害照受不打断
                return;
            }
            // —— 韧性破 ——
            // 硬直冷却检查（防 stagger-lock）：冷却内 → 韧性钳 0 不触发硬直（下次削韧再破才触发）。
            if (poise.StaggerCooldownUntilRound.TryGetValue(side, out int until) && round < until)
            {
                poise.Remaining[side] = 0;
                return;
            }
            // 触发硬直：注入 turns=1 stagger（TickDots 已过 → 天然拒止下回合 1 次 = 打断）。
            pendingControls.Add(new ControlEntry(side, "stagger", limits.StaggerDurationTurns));
            poise.StaggerCount.TryGetValue(side, out int priorStaggers);
            poise.StaggerCount[side] = priorStaggers + 1;
            poise.StaggerCooldownUntilRound[side] = round + limits.StaggerCooldown;
            // 韧性重置（抗性递减：越被打断上限越高，越难再破）。
            poise.Remaining[side] = StaggerResetPoise(poise.PoiseMax, priorStaggers, limits.StaggerDRStep);
        }

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

        /// <summary>
        /// 检查防方是否修了横练/护体类功法（门控反伤/格挡）。
        /// cv-007：提为 internal 供 <see cref="ResistanceProviders.ResistanceOf"/> 复用（判 BodyArtPhysResistBonus 加成）。
        /// 语义="已修横练/护体"（遍历 path.ArtCategories Role=body/defense 且 ChosenArtIds 命中）。
        /// </summary>
        internal static bool HasBodyArt(CultivationPathDef path, CultivationState st)
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
