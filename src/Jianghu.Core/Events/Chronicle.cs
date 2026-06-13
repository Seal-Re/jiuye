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
