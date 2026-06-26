using Jianghu.Model;

namespace Jianghu.Drama
{
    /// <summary>
    /// 戏剧引擎的**只读**世界视图 seam（drama-007，spec §2/§3）。
    /// DramaContext.Resolve 经此读世界状态求值谓词——与 World 解耦（可 mock 单测，零接线）。
    /// drama-010 由 World 实现（Power 同 SparAction/RuleBrain.SelfPower 公式；其余薄封装既有查询）。
    /// **只读**：戏剧层绝不经此 mutate（写经 IDramaMutator，drama-007b/011）。
    /// </summary>
    public interface IDramaView
    {
        /// <summary>角色战力 = Force×2 + Internal + Constitution（同 SparAction.Power / RuleBrain.SelfPower）。</summary>
        int Power(CharacterId who);

        /// <summary>有向好感 from→to（同 Relations.Affinity，[-100,100]）。</summary>
        int Affinity(CharacterId from, CharacterId to);

        /// <summary>角色在世（未寿尽/未退场）。</summary>
        bool IsAlive(CharacterId who);

        /// <summary>两角色当前同节点（狭路相逢判定）。</summary>
        bool SameNode(CharacterId a, CharacterId b);

        /// <summary>角色当前 Goal（drama-011：Director 存档原 Goal 供弧收束还原）。</summary>
        Goal GoalOf(CharacterId who);
    }
}
