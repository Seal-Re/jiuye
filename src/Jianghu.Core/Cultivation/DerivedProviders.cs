using System.Collections.Generic;
using Jianghu.Stats;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 4 per-path derived:* provider 实现（A.0 真派生, 解 derived 项恒0占位）。
    /// stockFirepower(符)/demonWeapon(魔)/wenGong(儒)/atavismFold(妖)。
    /// 纯整数确定性, 读 CultivationState.Resources + Flags, 不消费随机。
    /// 注册: 生成期/WorldFactory 调 DerivedRegistry.Register(key, provider)。
    /// </summary>
    public static class DerivedProviders
    {
        /// <summary>注册全部 5 derived provider。</summary>
        public static void RegisterAll()
        {
            DerivedRegistry.Register("stockFirepower", new StockFirepowerProvider());
            DerivedRegistry.Register("demonWeapon", new DemonWeaponProvider());
            DerivedRegistry.Register("wenGong", new WenGongProvider());
            DerivedRegistry.Register("atavismFold", new AtavismFoldProvider());
            DerivedRegistry.Register("arrayPower", new ArrayPowerProvider());
        }
    }

    /// <summary>
    /// derived:stockFirepower（符修·储备火力当量）:
    /// = talismanStore × 5（标量近似: 每张符平均档火力=5）。
    /// 真 per-grade Σ → FULLSTRUCT（需 gradeFirepower[] 数据结构）。
    /// </summary>
    internal sealed class StockFirepowerProvider : IDerivedProvider
    {
        public int Compute(CultivationState st, StatBlock stats)
        {
            // 简化: talismanStore 存量 × 5（平均每张 gradeFirepower=5）
            st.Resources.TryGetValue("talismanStore", out int store);
            return store * 5;
        }
    }

    /// <summary>
    /// derived:demonWeapon（魔修·炼魔器物power之和）:
    /// = MoGong / 2（魔功越深炼器越强, 折半入power）。
    /// 真 per-artifact Σ → FULLSTRUCT（需 ArtifactDef roster）。
    /// </summary>
    internal sealed class DemonWeaponProvider : IDerivedProvider
    {
        public int Compute(CultivationState st, StatBlock stats)
        {
            st.Resources.TryGetValue("MoGong", out int mg);
            return mg / 2;
        }
    }

    /// <summary>
    /// derived:wenGong（儒修·文宫merit）:
    /// = RealmIndex × 3 + haoran / 10（境界×文位基数 + 浩然气折）。
    /// 文宫=文位(bound to realm) + 浩然气积淀, 双双贡献 power。
    /// </summary>
    internal sealed class WenGongProvider : IDerivedProvider
    {
        public int Compute(CultivationState st, StatBlock stats)
        {
            st.Resources.TryGetValue("haoran", out int haoran);
            return st.RealmIndex * 3 + haoran / 10;
        }
    }

    /// <summary>
    /// derived:atavismFold（妖修·返祖折数）:
    /// = yaoDan / 5 + atavismDeg / 10（妖丹精纯度折 + 返祖退化折）。
    /// 返祖程度越深越强但 atavismDeg 越高越不稳定（双边杠杆）。
    /// </summary>
    internal sealed class AtavismFoldProvider : IDerivedProvider
    {
        public int Compute(CultivationState st, StatBlock stats)
        {
            st.Resources.TryGetValue("yaoDan", out int yd);
            st.Resources.TryGetValue("atavismDeg", out int ad);
            return yd / 5 + ad / 10;
        }
    }

    /// <summary>
    /// derived:arrayPower（阵修·在场阵power聚合）:
    /// = (compute + stones) × 2（灵石储量+算力折为在场阵法总威力）。
    /// 真 per-array Σ → FULLSTRUCT（需 array roster 结构）。
    /// </summary>
    internal sealed class ArrayPowerProvider : IDerivedProvider
    {
        public int Compute(CultivationState st, StatBlock stats)
        {
            st.Resources.TryGetValue("compute", out int cmp);
            st.Resources.TryGetValue("stones", out int stn);
            return (cmp + stn) * 2;
        }
    }
}
