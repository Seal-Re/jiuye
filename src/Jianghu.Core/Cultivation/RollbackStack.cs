using System;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 结算回滚栈（story fullstruct-007）。LIFO 栈，保存每次交锋前的 HP 与累计伤害快照，
    /// 支持因果逆演（撤销上次交锋）、夺舍续命（濒死回滚致命伤害）、分魂挡刀（伤害重定向）的结算回滚。
    /// 生活于 DuelEngine.ResolveR2 作用域，非持久字段。纯整数，确定性，无 RNG。
    /// </summary>
    public sealed class RollbackStack
    {
        /// <summary>栈深度上限（防无限 push）。</summary>
        public const int MaxDepth = 10;

        private readonly ExchangeSnapshot[] _stack = new ExchangeSnapshot[MaxDepth];
        private int _depth;

        /// <summary>当前栈深度（已 push 的 snapshot 数）。</summary>
        public int Depth => _depth;

        /// <summary>栈是否为空。</summary>
        public bool IsEmpty => _depth == 0;

        /// <summary>栈是否已满（达到 MaxDepth）。</summary>
        public bool IsFull => _depth >= MaxDepth;

        /// <summary>
        /// 压入一个交锋快照。栈满时返回 false（不抛异常，静默忽略）。
        /// </summary>
        public bool Push(ExchangeSnapshot snapshot)
        {
            if (_depth >= MaxDepth) return false;
            _stack[_depth++] = snapshot;
            return true;
        }

        /// <summary>
        /// 弹出最近一个交锋快照。栈空时返回 null（不抛异常）。
        /// </summary>
        public ExchangeSnapshot? Pop()
        {
            if (_depth == 0) return null;
            return _stack[--_depth];
        }

        /// <summary>清空栈（所有 snapshot 丢弃）。</summary>
        public void Clear()
        {
            _depth = 0;
        }
    }

    /// <summary>
    /// 单次交锋的快照（story fullstruct-007）。保存交锋前后的双方 HP 与伤害/反伤量，
    /// 供回滚栈还原。纯整数。
    /// </summary>
    public readonly struct ExchangeSnapshot
    {
        /// <summary>交锋前攻方 HP。</summary>
        public readonly int AttackerHpBefore;
        /// <summary>交锋前防方 HP。</summary>
        public readonly int DefenderHpBefore;
        /// <summary>本回合攻→防伤害（不含反伤）。</summary>
        public readonly int DmgToB;
        /// <summary>本回合防→攻伤害（不含反伤）。</summary>
        public readonly int DmgToA;
        /// <summary>本回合反伤到防方量。</summary>
        public readonly int ReflectToB;
        /// <summary>本回合反伤到攻方量。</summary>
        public readonly int ReflectToA;
        /// <summary>交锋前对防方累积伤害（用于平局判定回滚）。</summary>
        public readonly long TotalDmgToBBefore;
        /// <summary>交锋前对攻方累积伤害（用于平局判定回滚）。</summary>
        public readonly long TotalDmgToABefore;

        public ExchangeSnapshot(
            int attackerHpBefore, int defenderHpBefore,
            int dmgToB, int dmgToA, int reflectToB, int reflectToA,
            long totalDmgToBBefore, long totalDmgToABefore)
        {
            AttackerHpBefore = attackerHpBefore;
            DefenderHpBefore = defenderHpBefore;
            DmgToB = dmgToB;
            DmgToA = dmgToA;
            ReflectToB = reflectToB;
            ReflectToA = reflectToA;
            TotalDmgToBBefore = totalDmgToBBefore;
            TotalDmgToABefore = totalDmgToABefore;
        }
    }
}
