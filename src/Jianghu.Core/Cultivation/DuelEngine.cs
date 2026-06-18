using System;
using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Model;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// R2 战斗结算引擎（story-003 batch4）。接模块系统：HP=pe、选招经 tier/cost/gate、
    /// OnUse→OnDefend→软情境同时扣血。纯整数、确定性、无 RNG。
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
        public static Result ResolveR2(
            Character attacker, Character defender,
            CultivationPathDef attackerPath, CultivationPathDef defenderPath,
            PathRegistry registry, LimitsConfig limits,
            SituationalResolver? resolver,
            CombatSkillDef? attackerSkill, CombatSkillDef? defenderSkill)
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

            // —— AC 4.5：功法门控防御检查 ——
            bool defCanEvade = HasMovementArt(defenderPath, defender.Cultivation);
            bool defCanReflect = HasBodyArt(defenderPath, defender.Cultivation);

            // —— 造 CombatContext ——
            var ctx = new CombatContext(attacker.Cultivation, attackerPath, defender.Cultivation, defenderPath);

            // —— AC 4.1：选招（传入技能优先；未传入则自动选最优）——
            var aSkill = attackerSkill ?? SelectBestSkill(attacker.Cultivation, attackerPath);
            var dSkill = defenderSkill ?? SelectBestSkill(defender.Cultivation, defenderPath);
            aSkill = ValidateSkill(aSkill, attacker.Cultivation, attackerPath);
            dSkill = ValidateSkill(dSkill, defender.Cultivation, defenderPath);

            // —— AC 4.2：回合循环，双方同时互攻 ——
            int roundLimit = MaxRounds;
            long totalDmgToB = 0, totalDmgToA = 0;
            var pendingDots = new List<DotEntry>();
            var pendingControls = new List<ControlEntry>();
            for (int round = 0; round < roundLimit && hpA > 0 && hpB > 0; round++)
            {
                // 被控方伤害=0（控场效果）
                int effectiveDmgToB = IsControlled(pendingControls, Side.Defender) ? 0 : 1;
                int effectiveDmgToA = IsControlled(pendingControls, Side.Attacker) ? 0 : 1;

                // EP modifiers: weaken the affected side's combat power
                int effPeA = ctx.ApplyEPModifiers(Side.Attacker, peA);
                int effPeB = ctx.ApplyEPModifiers(Side.Defender, peB);

                var (dmgToB, reflectToA) = ResolveExchange(
                    aSkill, effPeA, defender.Cultivation,
                    attackerPath, defenderPath, ctx, limits, resolver,
                    Side.Attacker, Side.Defender, defCanEvade, defCanReflect,
                    pendingDots, pendingControls);
                dmgToB *= effectiveDmgToB;

                var (dmgToA, reflectToB) = ResolveExchange(
                    dSkill, effPeB, attacker.Cultivation,
                    defenderPath, attackerPath, ctx, limits, resolver,
                    Side.Attacker, Side.Defender,
                    HasMovementArt(attackerPath, attacker.Cultivation),
                    HasBodyArt(attackerPath, attacker.Cultivation),
                    pendingDots, pendingControls);
                dmgToA *= effectiveDmgToA;

                // 同时扣血（读 pre-HP）：反伤加到对应方向
                hpB = Math.Max(0, hpB - dmgToB - reflectToB);
                hpA = Math.Max(0, hpA - dmgToA - reflectToA);
                totalDmgToB += dmgToB + reflectToB;
                totalDmgToA += dmgToA + reflectToA;

                // —— AC 4.3：dot/control 回合间结算 ——
                TickDots(pendingDots, pendingControls, ref hpA, ref hpB);
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
            bool defCanEvade, bool defCanReflect,
            List<DotEntry> pendingDots, List<ControlEntry> pendingControls)
        {
            const int Scale = 100;
            long dmg = (long)attackerPe * Scale / BaseDamageDivisor;
            long totalReflect = 0;

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
                        int turns = op.Amount >= 1 ? op.Amount : 1;
                        pendingControls.Add(new ControlEntry(defenderSide, op.Key ?? "ctrl", turns));
                        continue;
                    }
                    int dmgUnscaled = (int)(dmg / Scale);
                    int result = ModuleResolver.ApplyOnUse(dmgUnscaled, op, ctx);
                    dmg = (long)result * Scale + (dmg % Scale);
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
                    if (op.Kind == EffectOpKind.Evade && !defCanEvade) continue;
                    if (op.Kind == EffectOpKind.ReflectDamage && !defCanReflect) continue;
                    int dmgUnscaled = (int)(dmg / Scale);
                    int result = ModuleResolver.ApplyOnDefend(dmgUnscaled, op, ctx, defenderSide, out int reflectDmg);
                    dmg = (long)result * Scale + (dmg % Scale);
                    totalReflect += reflectDmg;
                }
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
    }
}
