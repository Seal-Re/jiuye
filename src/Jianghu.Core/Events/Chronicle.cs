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
