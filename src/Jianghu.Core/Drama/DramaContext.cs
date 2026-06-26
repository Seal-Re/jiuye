using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Drama
{
    /// <summary>
    /// storylet 谓词求值上下文（drama-007，spec §2/§3/§4）。唯一 Resolve(RoleRef,DramaVar)→int
    /// 纯映射（仿 World.BuildContext），经 IDramaView 读世界态。整数比较 Eval + AllPass 全 AND。
    /// **纯函数、无副作用**（写经 IDramaMutator，drama-007b/011）。
    ///
    /// 角色解析：弧有 (Avenger=Holder, Target)。RoleRef.Holder/Self→Avenger；Target→Target。
    /// </summary>
    public sealed class DramaContext
    {
        private readonly IDramaView _view;
        private readonly ArcInstance _arc;
        private readonly Grudge? _grudge; // 点火相评估时账本恩怨；纯推进相可空（GrudgeIntensity→0）

        public DramaContext(IDramaView view, ArcInstance arc, Grudge? grudge = null)
        {
            _view = view;
            _arc = arc;
            _grudge = grudge;
        }

        /// <summary>RoleRef → 具体 CharacterId（Holder/Self=复仇者，Target=仇人）。</summary>
        private CharacterId Who(RoleRef role)
            => role == RoleRef.Target ? _arc.Target : _arc.Avenger;

        /// <summary>RoleRef 的「对方」（Affinity 的 to 端）。</summary>
        private CharacterId Other(RoleRef role)
            => role == RoleRef.Target ? _arc.Avenger : _arc.Target;

        /// <summary>
        /// 唯一世界状态映射：(subject, var) → 整数。全整数（B.2），无浮点。
        /// </summary>
        public int Resolve(RoleRef subject, DramaVar var)
        {
            switch (var)
            {
                case DramaVar.Power:
                    return _view.Power(Who(subject));
                case DramaVar.Affinity:
                    return _view.Affinity(Who(subject), Other(subject));
                case DramaVar.GrudgeIntensity:
                    return _grudge != null ? _grudge.Intensity : 0;
                case DramaVar.SameNode:
                    return _view.SameNode(_arc.Avenger, _arc.Target) ? 1 : 0;
                case DramaVar.TargetAlive:
                    return _view.IsAlive(_arc.Target) ? 1 : 0;
                default:
                    return 0;
            }
        }

        /// <summary>单谓词整数比较：Resolve(subject,var) op Threshold。</summary>
        public bool Eval(Predicate p)
        {
            int lhs = Resolve(p.Subject, p.Var);
            switch (p.Op)
            {
                case CmpOp.Ge: return lhs >= p.Threshold;
                case CmpOp.Le: return lhs <= p.Threshold;
                case CmpOp.Eq: return lhs == p.Threshold;
                case CmpOp.Gt: return lhs > p.Threshold;
                case CmpOp.Lt: return lhs < p.Threshold;
                default: return false;
            }
        }

        /// <summary>全 AND：空表→true；任一 false→短路 false。</summary>
        public bool AllPass(IReadOnlyList<Predicate> preds)
        {
            for (int i = 0; i < preds.Count; i++)
                if (!Eval(preds[i])) return false;
            return true;
        }
    }
}
