using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jianghu.Actions;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Stats;
using Jianghu.Sim;

// —— 参数解析：位置参 [seed] [steps] 保持既有语义；--cultivation 开关（默认 off → 逐字节既有行为）——
bool cultivation = args.Any(a => a == "--cultivation");
// story-008：--map / --faction 开关（默认 off → 不激活，逐字节既有行为）。
bool mapOn = args.Any(a => a == "--map");
bool factionOn = args.Any(a => a == "--faction");
// drama-010：--drama 开关（默认 off → 不激活，逐字节既有行为）。
bool dramaOn = args.Any(a => a == "--drama");
// drama-013：--drama-feuds 预置冤孽（需 --drama；演示用，预置强恩怨 + 师徒边）。
bool dramaSeedFeuds = args.Any(a => a == "--drama-feuds");
// gh-004：--record <file> 录制命令序列；--replay <file> 重放命令序列。
string? recordPath = GetOptArg(args, "--record");
string? replayPath = GetOptArg(args, "--replay");
var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
ulong seed = positional.Length > 0 && ulong.TryParse(positional[0], out var s) ? s : 2026UL;
int steps = positional.Length > 1 && int.TryParse(positional[1], out var n) ? n : 200;
int budget = 8;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (!cultivation)
{
    RunLegacy(seed, steps, budget, mapOn, factionOn, dramaOn, dramaSeedFeuds);
    return;
}

RunCultivation(seed, steps, budget, mapOn, factionOn, dramaOn, dramaSeedFeuds,
    recordPath: recordPath, replayPath: replayPath);

// gh-004：提取 --key value 形式的可选参数
static string? GetOptArg(string[] args, string key)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == key) return args[i + 1];
    return null;
}

// —— 既有开放式江湖演示（cultivation-off，与 v1.0 逐字节）——
static void RunLegacy(ulong seed, int steps, int budget, bool mapOn, bool factionOn, bool dramaOn, bool dramaSeedFeuds)
{
    var world = WorldFactory.CreateInitial(seed, LimitsConfig.Default, initialCount: 8, mapOn: mapOn, factionOn: factionOn, dramaOn: dramaOn, dramaSeedFeuds: dramaSeedFeuds);
    Console.WriteLine($"=== 江湖开演 (seed={seed}, steps={steps}{(mapOn ? ", map=on" : "")}{(factionOn ? ", faction=on" : "")}{(dramaOn ? ", drama=on" : "")}) ===");
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
}

// —— 修炼江湖演示（cultivation-on）：21 路皆能定路 / 突破 / per-path 战力分化 / 软情境 ——
static void RunCultivation(ulong seed, int steps, int budget, bool mapOn, bool factionOn, bool dramaOn, bool dramaSeedFeuds,
    string? recordPath = null, string? replayPath = null)
{
    var world = WorldFactory.CreateInitial(seed, LimitsConfig.Default, initialCount: 8, cultivation: true, mapOn: mapOn, factionOn: factionOn, dramaOn: dramaOn, dramaSeedFeuds: dramaSeedFeuds);
    // 展示用注册表（独立于 World 内部 registry，仅供 PathId→路名/境界名/战力查询）。
    var registry = new PathRegistry(new CodePathSource());

    // gh-004：重放模式——加载命令序列，替代 RuleBrain
    if (replayPath != null)
    {
        var json = File.ReadAllText(replayPath);
        var commands = JsonSerializer.Deserialize<List<CommandIntent>>(json)
                       ?? throw new InvalidOperationException("Failed to deserialize command log");
        world.SetReplay(commands);
        Console.WriteLine($"=== 修炼江湖重放 (seed={seed}, steps={steps}, commands={commands.Count}) ===");
    }
    else
    {
        Console.WriteLine($"=== 修炼江湖开演 (seed={seed}, steps={steps}, cultivation=on{(mapOn ? ", map=on" : "")}{(factionOn ? ", faction=on" : "")}{(dramaOn ? ", drama=on" : "")}) ===");
    }

    // gh-004：录制模式——启用 CommandLog
    if (recordPath != null)
        world.CommandLog = new List<CommandIntent>();

    for (int i = 0; i < steps; i++) world.Advance(budget);

    // gh-004：录制完成 → 写 JSON
    if (recordPath != null && world.CommandLog != null)
    {
        var json = JsonSerializer.Serialize(world.CommandLog, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(recordPath, json);
        Console.WriteLine($"命令序列已录制: {recordPath} ({world.CommandLog.Count} 条)");
    }

    var lines = world.Chronicle.Lines;

    // —— 入道：PathEntered（谁入了哪条路）——
    var entered = lines.Where(l => l.Contains("拜入") && l.Contains("一脉")).ToList();
    Console.WriteLine($"\n--- 入道录（PathEntered，共 {entered.Count} 人入道）---");
    foreach (var l in entered.Take(8)) Console.WriteLine(RenderPath(l, registry));

    // —— 破境：RealmBreakthrough（谁突破到第几境）——
    var broke = lines.Where(l => l.Contains("冲破瓶颈")).ToList();
    Console.WriteLine($"\n--- 破境录（RealmBreakthrough，共 {broke.Count} 次突破，节选末 8）---");
    foreach (var l in broke.Skip(Math.Max(0, broke.Count - 8))) Console.WriteLine(l);

    // —— 切磋：DuelResolved（per-path 战力分化）——
    var duels = lines.Where(l => l.Contains("切磋")).ToList();
    Console.WriteLine($"\n--- 切磋录（DuelResolved，共 {duels.Count} 场，节选末 6）---");
    foreach (var l in duels.Skip(Math.Max(0, duels.Count - 6))) Console.WriteLine(l);

    // —— 门派录：FactionPromoted 晋升（story-010）+ TerritoryLost 夺地（story-011）——仅 faction-on 有数据 ——
    var promos = lines.Where(l => l.Contains("晋升")).ToList();
    if (promos.Count > 0)
    {
        Console.WriteLine($"\n--- 门派录·晋升（FactionPromoted，共 {promos.Count} 次，节选末 6）---");
        foreach (var l in promos.Skip(Math.Max(0, promos.Count - 6))) Console.WriteLine(l);
    }
    var conquests = lines.Where(l => l.Contains("攻取")).ToList();
    if (conquests.Count > 0)
    {
        Console.WriteLine($"\n--- 门派录·夺地（TerritoryLost，共 {conquests.Count} 次，节选末 6）---");
        foreach (var l in conquests.Skip(Math.Max(0, conquests.Count - 6))) Console.WriteLine(l);
    }

    // —— 恩怨录：drama 层叙事投影（--drama 才有数据；引擎已产于 Chronicle，此处显示）——
    //    立誓复仇(ArcIgnited)/手刃仇人(RevengeConsummated)/父债子偿(GrudgeInherited)/结怨(GrudgeFormed)/弧推进(ArcStageEntered)。
    //    匹配 Chronicle.cs 投影关键词；纯显示，不改模拟。
    var dramaLines = lines.Where(l =>
        l.Contains("立誓复仇") || l.Contains("手刃仇人") || l.Contains("父债子偿")
        || l.Contains("结怨") || l.Contains("复仇弧") || l.Contains("复仇未竟")
        || l.Contains("之仇") || l.Contains("讨还血债")).ToList();
    if (dramaLines.Count > 0)
    {
        Console.WriteLine($"\n--- 恩怨录（Drama：恩怨/复仇/跨代继承，共 {dramaLines.Count} 条，节选末 10）---");
        foreach (var l in dramaLines.Skip(Math.Max(0, dramaLines.Count - 10))) Console.WriteLine(l);
    }

    // —— 当世修士群像：per-path 战力分化（不同路同四维 → 不同 EffectivePower）——
    var cultivators = world.AliveCharacters().Where(c => c.Cultivation != null).ToList();
    Console.WriteLine($"\n--- 当世修士 {cultivators.Count} 人（per-path 战力分化）---");
    foreach (var c in cultivators.Take(8))
    {
        var st = c.Cultivation!;
        var def = registry.ById(st.PathId);
        int realmIdx = st.RealmIndex;
        string realmName = realmIdx < def.Curve.RealmNames.Count ? RealmQuery.Describe(def.Curve, realmIdx).Display : $"第{realmIdx}重";
        int pe = PowerEngine.Evaluate(st, c.Stats, def, LimitsConfig.Default);
        Console.WriteLine(
            $"【{c.Persona.Name}】{def.Name}({st.PathId}) · {realmName}(realm{realmIdx}) · " +
            $"武{c.Stats.Get(StatKind.Force)}内{c.Stats.Get(StatKind.Internal)}根{c.Stats.Get(StatKind.Constitution)}悟{c.Stats.Get(StatKind.Insight)} → 战力 {pe}");
    }

    // —— 21 路可定证：本局被定到的路 / 全 21 路覆盖度 ——
    var seenPaths = new SortedSet<string>(cultivators.Select(c => c.Cultivation!.PathId), StringComparer.Ordinal);
    Console.WriteLine($"\n--- 本局定到 {seenPaths.Count} 条路（全注册 {registry.All.Count} 路；跨局采样覆盖全 21）---");
    Console.WriteLine("    " + string.Join("、", seenPaths.Select(p => registry.ById(p).Name)));
}

// PathEntered 行追加路名（PathId → 中文路名），人眼可读。
static string RenderPath(string line, PathRegistry registry)
{
    foreach (var p in registry.All)
        if (line.Contains(p.PathId))
            return line.Replace(p.PathId, $"{p.Name}({p.PathId})");
    return line;
}
