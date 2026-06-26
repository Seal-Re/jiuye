using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Events
{
    /// <summary>DomainEvent 的只读文本投影（武侠味模板）。name 解析器把 Id 映射为称谓。</summary>
    public sealed class Chronicle
    {
        private readonly List<string> _lines = new List<string>();
        public IReadOnlyList<string> Lines => _lines.AsReadOnly();
        public int Count => _lines.Count;

        public void Append(DomainEvent e, Func<CharacterId, string> name)
            => Append(e, name, null);

        /// <summary>
        /// realmDesc（可空）：境界 flatIndex → 「大境界·小境界（UT）」展示串（A1.5，调用方注 RealmQuery）。
        /// 为 null 时 RealmBreakthrough 回退裸整数「第 N 重」——off 无此事件，故 off 逐字节不受扰。
        /// </summary>
        public void Append(DomainEvent e, Func<CharacterId, string> name, Func<int, string>? realmDesc)
        {
            string text;
            switch (e)
            {
                case CharacterBorn b: text = $"[{b.Tick}] {name(b.Id)} 踏入江湖。"; break;
                case CharacterTrained t: text = $"[{t.Tick}] {name(t.Id)} 闭关苦修，{t.Stat} 精进 {t.Delta}。"; break;
                case CharacterTraveled v: text = $"[{v.Tick}] {name(v.Id)} 自 {v.From.Value} 行至 {v.To.Value}。"; break;
                case DuelResolved d: text = $"[{d.Tick}] {name(d.Winner)} 与 {name(d.Loser)} 切磋，胜（差 {d.Margin}）。"; break;
                case RelationChanged r: text = $"[{r.Tick}] {name(r.From)} 对 {name(r.To)} 情谊变为 {r.NewValue}。"; break;
                case CharacterDied x: text = $"[{x.Tick}] {name(x.Id)} 寿尽，享年 {x.Age}，江湖再无此人。"; break;
                case PathEntered p: text = $"[{p.Tick}] {name(p.Id)} 拜入 {p.PathId} 一脉，自此踏上修行路。"; break;
                case RealmBreakthrough rb:
                    text = realmDesc != null
                        ? $"[{rb.Tick}] {name(rb.Id)} 冲破瓶颈，跻身 {realmDesc(rb.NewRealmIndex)} 之境。"
                        : $"[{rb.Tick}] {name(rb.Id)} 冲破瓶颈，境界精进至第 {rb.NewRealmIndex} 重。";
                    break;
                case FactionPromoted fp:
                {
                    string rank = fp.NewRank switch { 1 => "内门弟子", 2 => "核心长老", 3 => "一派掌门", _ => "外门弟子" };
                    text = $"[{fp.Tick}] {name(fp.Id)} 因功勋卓著，晋升为 {rank}。";
                    break;
                }
                case TerritoryLost tl:
                    text = $"[{tl.Tick}] 门派#{tl.ToFaction} 攻取门派#{tl.FromFaction} 的 {tl.Site} 号地，两派自此结怨。";
                    break;
                // —— 戏剧引擎 B（drama-008）：武侠味投影，仅渲染层不进数值路径（B.8）——
                case GrudgeFormed gf:
                {
                    string kind = gf.Kind switch
                    {
                        Drama.GrudgeKind.Insult => "羞辱之仇",
                        Drama.GrudgeKind.Maiming => "残身之仇",
                        Drama.GrudgeKind.Slaughter => "灭门血仇",
                        _ => "怨仇",
                    };
                    text = $"[{gf.Tick}] {name(gf.Holder)} 与 {name(gf.Target)} 结下 {kind}（恨意 {gf.Intensity}）。";
                    break;
                }
                case GrudgeInherited gi:
                    text = $"[{gi.Tick}] {name(gi.Heir)} 继承先人遗志，誓向 {name(gi.Target)} 讨还血债——父债子偿，已是第 {gi.Generation} 代恩怨（恨意 {gi.Intensity}）。";
                    break;
                case ArcIgnited ai:
                    text = $"[{ai.Tick}] {name(ai.Avenger)} 立誓复仇，踏上追讨 {name(ai.Target)} 的不归路。";
                    break;
                case ArcStageEntered ase:
                {
                    string stage = ase.Stage switch
                    {
                        Drama.ArcStage.Victimized => "蒙难含冤",
                        Drama.ArcStage.BuildUp => "闭关蓄力",
                        Drama.ArcStage.Hunting => "四处寻仇",
                        Drama.ArcStage.Showdown => "狭路相逢",
                        Drama.ArcStage.Resolved => "恩怨了结",
                        Drama.ArcStage.Abandoned => "复仇未竟",
                        _ => "复仇路上",
                    };
                    text = $"[{ase.Tick}] 复仇弧#{ase.Arc.Value} 进入「{stage}」。";
                    break;
                }
                case RevengeConsummated rc:
                    text = rc.AvengerPrevailed
                        ? $"[{rc.Tick}] {name(rc.Avenger)} 于决战中手刃仇人 {name(rc.Target)}，大仇得报，恩怨自此了断。"
                        : $"[{rc.Tick}] {name(rc.Avenger)} 寻 {name(rc.Target)} 决战，奈何技不如人，饮恨当场。";
                    break;
                case ArcAbandoned aa:
                    text = $"[{aa.Tick}] 复仇弧#{aa.Arc.Value} 半途而废（{aa.Reason}），恩怨随风消散。";
                    break;
                default: text = $"[{e.Tick}] (未知事件)"; break;
            }
            _lines.Add(text);
        }

        public Chronicle Clone()
        {
            var c = new Chronicle();
            c._lines.AddRange(_lines);
            return c;
        }
    }
}
