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
            bool WasAutoWin, int AttackerHpRemaining, int DefenderHpRemaining);

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
                    int.MaxValue, // 碾压无实际 margin
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
            for (int round = 0; round < roundLimit && hpA > 0 && hpB > 0; round++)
            {
                int dmgToB = ResolveExchange(
                    aSkill, peA, defender.Cultivation,
                    attackerPath, defenderPath, ctx, limits, resolver,
                    Side.Attacker, Side.Defender, defCanEvade, defCanReflect);

                int dmgToA = ResolveExchange(
                    dSkill, peB, attacker.Cultivation,
                    defenderPath, attackerPath, ctx, limits, resolver,
                    Side.Attacker, Side.Defender,
                    HasMovementArt(attackerPath, attacker.Cultivation),
                    HasBodyArt(attackerPath, attacker.Cultivation));

                // 同时扣血（读 pre-HP）
                hpB = Math.Max(0, hpB - dmgToB);
                hpA = Math.Max(0, hpA - dmgToA);
                totalDmgToB += dmgToB;
                totalDmgToA += dmgToA;

                // —— AC 4.3：dot/control 回合间结算 ——
                TickDots(ctx, Side.Attacker);
                TickDots(ctx, Side.Defender);
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
                DefenderHpRemaining: hpB);
        }

        /// <summary>
        /// 单次交锋结算：攻方 OnUse→防方 OnDefend→软情境→伤害返回。
        /// </summary>
        private static int ResolveExchange(
            CombatSkillDef? skill,
            int attackerPe,
            CultivationState defenderState,
            CultivationPathDef attackerPath, CultivationPathDef defenderPath,
            CombatContext ctx, LimitsConfig limits, SituationalResolver? resolver,
            Side attackerSide, Side defenderSide,
            bool defCanEvade, bool defCanReflect)
        {
            // 基础伤害（scaled ×100 防截断，situational adj 后除回）
            const int Scale = 100;
            long dmg = (long)attackerPe * Scale / BaseDamageDivisor;

            // OnUse：经 ModuleResolver 逐模块修正（模块操作 scaled dmg）
            if (skill != null)
            {
                foreach (var op in skill.OnUse)
                {
                    int dmgUnscaled = (int)(dmg / Scale);
                    int result = ModuleResolver.ApplyOnUse(dmgUnscaled, op, ctx);
                    dmg = (long)result * Scale + (dmg % Scale);
                }
            }

            // OnDefend：防方防御模块（经 ModuleResolver.ApplyOnDefend）
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
                    int result = ModuleResolver.ApplyOnDefend(dmgUnscaled, op, ctx, defenderSide);
                    dmg = (long)result * Scale + (dmg % Scale);
                }
            }

            // 软情境修正（在 scaled dmg 上做，避免小值截断）
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

            return (int)Math.Max(0, dmg / Scale);
        }

        /// <summary>回合间 dot/control 结算（AC 4.3）。</summary>
        private static void TickDots(CombatContext ctx, Side side)
        {
            // dot/control 模块的回合间结算
            // 持续伤：每回合扣 HP（经 chokepoint）
            // 控场：减少剩余回合数
            // 当前简化：dot/control 在 batch4 中只做占位，完整逻辑在后续 story
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

        /// <summary>Validate skill: tier≤realm & TryPayCost; returns null if invalid (downgrade to bare attack).</summary>
        private static CombatSkillDef? ValidateSkill(CombatSkillDef? skill, CultivationState st, CultivationPathDef path)
        {
            if (skill == null) return null;
            if (skill.Tier > st.RealmIndex) return null; // tier gate
            // TryPayCost: 全或无扣资源
            if (skill.Cost.Count > 0 && !EffectInterpreter.TryPayCost(skill.Cost, st))
                return null;
            return skill;
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
