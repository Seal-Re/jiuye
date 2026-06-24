using System;
using System.Collections.Generic;
using System.Linq;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 21 路道心注册表（A3 §3 + A123 §A.2.3）。
    /// 数据驱动：加路=加数据行。通过 <c>ById</c> 按路线查询。
    /// </summary>
    public sealed class DaoHeartRegistry
    {
        private readonly Dictionary<string, DaoHeartDef> _byPathId;

        public DaoHeartRegistry()
        {
            _byPathId = new Dictionary<string, DaoHeartDef>(BuildAll());
        }

        /// <summary>按路线 ID 查道心定义。未注册路线抛 <see cref="KeyNotFoundException"/>。</summary>
        public DaoHeartDef ById(string pathId) => _byPathId[pathId];

        /// <summary>全部注册路线数（= 21）。</summary>
        public int Count => _byPathId.Count;

        /// <summary>是否已注册该路线。</summary>
        public bool Contains(string pathId) => _byPathId.ContainsKey(pathId);

        /// <summary>21 路 daoHeart_init 乘子表。A3 §3 基准 12 路 + A123 §A.2.3 扩展 9 路。</summary>
        private static Dictionary<string, DaoHeartDef> BuildAll()
        {
            var list = new List<DaoHeartDef>
            {
                // ===== A3 §3 基准 12 路 =====
                new("sword_immortal", 2, Gains(("剑心通明",3),("悟剑",4),("斩执念",5)), Demons(("剑痴",3),("嗜杀",2),("执念反噬",4))),
                new("ti_xiu_hengshi", 2, Gains(("肉身成圣",3),("炼体悟道",4),("炁通百骸",3)), Demons(("炼体过载",4),("气血逆行",3),("力竭",2))),
                new("fa_xiu", 2, Gains(("道法自然",4),("参悟天机",5),("符法归一",3)), Demons(("法术反噬",3),("走火入魔",4),("识海枯竭",2))),
                new("array_formation", 2, Gains(("阵道精进",3),("化阵入微",3),("推演阵图",4)), Demons(("阵基崩坏",3),("反噬困阵",3),("灵力枯竭",2))),
                new("qixiu_artificer", 2, Gains(("器道精研",3),("炼器悟真",3),("万宝归心",4)), Demons(("炼器失败",3),("器灵反噬",4),("材料耗尽",2))),
                new("soul_divine_sense", 3, Gains(("神魂澄澈",5),("观想凝神",4),("破妄见真",6)), Demons(("神识过载",3),("魂飞魄散",2),("心魔侵魂",5))),
                new("ming_fate_causality", 2, Gains(("命理推演",4),("窥见天机",5),("顺天应命",3)), Demons(("天机反噬",4),("命格错乱",3),("逆天罚",5))),
                new("dan_xiu", 2, Gains(("丹道通玄",4),("炼药悟真",3),("丹成见道",5)), Demons(("丹毒积累",3),("炸炉反噬",4),("药性失控",2))),
                new("gui_xiu_yang_hun", 2, Gains(("阴魂凝实",3),("鬼道通玄",4),("炼魂悟真",5)), Demons(("魂体崩溃",4),("阴煞噬主",5),("阳气侵蚀",3))),
                new("buddhist_golden_body", 3, Gains(("佛法精进",5),("金刚持戒",6),("度化众生",7)), Demons(("破戒",5),("魔考",6),("执相",3))),
                new("lei_xiu", 2, Gains(("雷霆正心",4),("天雷淬体",5),("雷法通玄",3)), Demons(("雷劫反噬",5),("灵力暴走",3),("天谴",4))),
                new("yu_shou", 2, Gains(("兽魂共鸣",4),("驭兽通灵",3),("万兽朝宗",5)), Demons(("兽性反噬",4),("灵兽叛离",3),("野性失控",5))),

                // ===== A123 §A.2.3 扩展 9 路 =====
                new("ru_xiu_haoran", 2, Gains(("养气",5),("教化",6),("善行",4)), Demons(("失德",3),("浩然枯",2),("文宫倾覆",4))),
                new("mo_xiu_xinmo", 2, Gains(("凝魔心",2),("守心",3),("魔道证真",4)), Demons(("噬心反噬",5),("堕魔",3),("天魔考",6))),
                new("yao_xiu_huaxing", 2, Gains(("化形得道",5),("守灵台",3),("妖丹凝华",4)), Demons(("兽性嗜杀",4),("化形失控",3),("天劫降妖",5))),
                new("xue_xiu_xuesha", 2, Gains(("凝血神",2),("收敛杀心",3),("血道通幽",4)), Demons(("血煞过载",5),("杀业",4),("血脉反噬",6))),
                new("du_gu_xiu", 2, Gains(("控蛊定心",4),("炼毒通玄",3),("蛊道大成",5)), Demons(("蛊噬主",5),("毒侵心",3),("蛊群失控",4))),
                new("fu_xiu_fulu", 2, Gains(("符道通玄",5),("符箓精研",3),("朱书悟道",4)), Demons(("符箓反噬",2),("储备耗尽",2),("灵墨干涸",3))),
                new("kuilei_shi", 2, Gains(("机心通玄",5),("人偶相照",4),("机关造化",3)), Demons(("机心失控",3),("神识带宽过载",2),("傀儡反噬",4))),
                new("yin_xiu_yuedao", 2, Gains(("心境澄明",5),("乐道",4),("天音入道",6)), Demons(("心乱",3),("入魔音",3),("五音崩坏",4))),
                new("yinguo_faze", 2, Gains(("勘破因果",4),("顺天",3),("因果圆满",5)), Demons(("天谴债",5),("逆天反噬",4),("业力缠身",6))),
            };

            var dict = new Dictionary<string, DaoHeartDef>();
            foreach (var def in list)
                dict[def.PathId] = def;
            return dict;
        }

        private static DaoHeartGain[] Gains(params (string source, int amount)[] items)
        {
            var arr = new DaoHeartGain[items.Length];
            for (int i = 0; i < items.Length; i++)
                arr[i] = new DaoHeartGain(items[i].source, items[i].amount);
            return arr;
        }

        private static InnerDemonSource[] Demons(params (string source, int amount)[] items)
        {
            var arr = new InnerDemonSource[items.Length];
            for (int i = 0; i < items.Length; i++)
                arr[i] = new InnerDemonSource(items[i].source, items[i].amount);
            return arr;
        }
    }
}
