using Godot;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Sim;

namespace GodotHost;

/// <summary>
/// Core→Godot 桥接节点。ADR-0004 §①+§② 落地：
/// §① 拉取 Core DomainEvent/快照增量 → Godot [Signal] 广播 → GD.Print
/// §② 固定时间步累加器：_Process(delta) 累加 → World.Advance() 确定性驱动
///
/// delta（浮点帧时）绝不进 Core（守 B.2）。Advance 由逻辑步驱动非帧率驱动 →
/// 掉帧只影响追帧次数，不改每步确定性轨迹。
/// </summary>
public partial class WorldBridge : Node
{
    // —— 固定时间步配置（adr-0004 §②）——
    private const double SimStepSeconds = 1.0;   // 1 秒 / 逻辑步
    private const int MaxCatchupSteps = 5;        // 每帧最大追帧数（防掉帧螺旋）

    // —— 测试/headless 参数 ——
    private const ulong WorldSeed = 42;
    private const int InitialCount = 30;
    private const int MaxSteps = 10;              // headless 测试用：跑完 N 步自动退出；0 = 不限

    /// <summary>Core 世界实例（公开——供 WorldView 等 View 节点只读访问）。</summary>
    public World World => _world;

    /// <summary>Core 事件行（每行一条事件文本）</summary>
    [Signal]
    public delegate void OnDomainEventEventHandler(string eventLine);

    /// <summary>全状态快照（StateSnapshot.Capture 确定性文本）</summary>
    [Signal]
    public delegate void OnSnapshotEventHandler(string snapshotText);

    private World _world = null!;
    private double _accumulator;
    private int _stepCount;
    private int _prevLineCount;

    /// <summary>
    /// 插值因子（0→1），供 View 层动画/UI 使用。
    /// 只读——绝不反向写 Core（B.2 守）。
    /// </summary>
    public float InterpolationFactor => (float)(_accumulator / SimStepSeconds);

    public override void _Ready()
    {
        GD.Print($"[WorldBridge] _Ready — 固定时间步累加器模式 " +
                 $"(SimStep={SimStepSeconds}s, MaxCatchup={MaxCatchupSteps})...");

        var limits = new LimitsConfig();
        _world = WorldFactory.CreateInitial(
            seed: WorldSeed, limits, initialCount: InitialCount,
            cultivation: true,
            mapOn: true,
            factionOn: false,
            dramaOn: false
        );

        _accumulator = 0.0;
        _stepCount = 0;
        _prevLineCount = _world.Chronicle.Lines.Count;
    }

    public override void _Process(double delta)
    {
        // 累加帧时（浮点 delta 仅在宿主侧，不进 Core —— B.2 守）
        _accumulator += delta;

        int caughtUp = 0;
        while (_accumulator >= SimStepSeconds && caughtUp < MaxCatchupSteps)
        {
            // headless 测试：跑完 MaxSteps 自动退出
            if (MaxSteps > 0 && _stepCount >= MaxSteps)
            {
                var snap = StateSnapshot.Capture(_world);
                GD.Print(snap);
                EmitSignal(SignalName.OnSnapshot, snap);
                GD.Print($"[WorldBridge] 完成：{_stepCount} 步，" +
                         $"{_world.Chronicle.Lines.Count} 条事件，" +
                         $"插值因子={InterpolationFactor:F3}");
                GetTree().Quit();
                return;
            }

            // 一步逻辑推进（Advance 参数不含 delta —— B.2 守）
            _world.Advance(budget: 1);

            // 广播新事件
            var lines = _world.Chronicle.Lines;
            for (int j = _prevLineCount; j < lines.Count; j++)
            {
                var line = lines[j];
                GD.Print(line);
                EmitSignal(SignalName.OnDomainEvent, line);
            }
            _prevLineCount = lines.Count;

            _accumulator -= SimStepSeconds;
            _stepCount++;
            caughtUp++;
        }

        // 追帧上限触发 → 重置累加器防螺旋（丢弃积压帧时）
        if (caughtUp >= MaxCatchupSteps && _accumulator >= SimStepSeconds)
        {
            _accumulator = 0.0;
            GD.Print($"[WorldBridge] 追帧上限触发（{MaxCatchupSteps} 步/帧），丢弃积压帧时");
        }
    }
}
