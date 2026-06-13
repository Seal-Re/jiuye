using System;
using System.Linq;
using Jianghu.Config;
using Jianghu.Stats;
using Jianghu.Sim;

ulong seed = args.Length > 0 && ulong.TryParse(args[0], out var s) ? s : 2026UL;
int steps = args.Length > 1 && int.TryParse(args[1], out var n) ? n : 200;
int budget = 8;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var world = WorldFactory.CreateInitial(seed, LimitsConfig.Default, initialCount: 8);
Console.WriteLine($"=== 江湖开演 (seed={seed}, steps={steps}) ===");
for (int i = 0; i < steps; i++) world.Advance(budget);  // 开放式：可改 while(true) 长跑

var lines = world.Chronicle.Lines;
Console.WriteLine($"\n--- 江湖编年史（共 {lines.Count} 条，节选末 30 条）---");
for (int i = Math.Max(0, lines.Count - 30); i < lines.Count; i++) Console.WriteLine(lines[i]);

var alive = world.AliveCharacters();
Console.WriteLine($"\n--- 当世人物 {alive.Count} / 逝者 {world.Deceased.Count} ---");
foreach (var c in alive.Take(5))
{
    int power = c.Stats.Get(StatKind.Force) * 2 + c.Stats.Get(StatKind.Internal) + c.Stats.Get(StatKind.Constitution);
    Console.WriteLine($"【{c.Persona.Name}·{c.Persona.Title}】{c.Persona.Origin} {c.Persona.Archetype} | 目标 {c.Goal.Kind}");
    Console.WriteLine($"    武{c.Stats.Get(StatKind.Force)} 内{c.Stats.Get(StatKind.Internal)} 根{c.Stats.Get(StatKind.Constitution)} 悟{c.Stats.Get(StatKind.Insight)} (战力{power}) · 寿限{c.Lifespan} · 阅历{c.RecallMemory().Count}条");
}
