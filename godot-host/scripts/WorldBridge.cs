using System;
using System.Collections.Generic;
using Godot;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Events;
using Jianghu.Sim;

namespace GodotHost;

/// <summary>
/// Core→Godot 桥接节点。ADR-0004 §① 落地：
/// 拉取 Core DomainEvent/快照增量 → Godot [Signal] 广播 → GD.Print。
/// Core 不感知 Godot（单向流，Model→View）。
/// </summary>
public partial class WorldBridge : Node
{
    /// <summary>Core 事件行（每行一条事件文本）</summary>
    [Signal]
    public delegate void OnDomainEventEventHandler(string eventLine);

    /// <summary>全状态快照（StateSnapshot.Capture 确定性文本）</summary>
    [Signal]
    public delegate void OnSnapshotEventHandler(string snapshotText);

    private World _world = null!;

    public override void _Ready()
    {
        GD.Print("[WorldBridge] _Ready — 初始化世界...");

        var limits = new LimitsConfig();
        _world = WorldFactory.CreateInitial(
            seed: 42, limits, initialCount: 30,
            cultivation: true,
            mapOn: false,
            factionOn: false,
            dramaOn: false
        );

        // 跑 N 步，每步打印新事件
        const int steps = 10;
        var prevLineCount = _world.Chronicle.Lines.Count;

        for (int i = 0; i < steps; i++)
        {
            _world.Advance(budget: 1);

            var lines = _world.Chronicle.Lines;
            for (int j = prevLineCount; j < lines.Count; j++)
            {
                var line = lines[j];
                GD.Print(line);
                EmitSignal(SignalName.OnDomainEvent, line);
            }
            prevLineCount = lines.Count;
        }

        // 全状态快照
        var snap = StateSnapshot.Capture(_world);
        GD.Print(snap);
        EmitSignal(SignalName.OnSnapshot, snap);

        GD.Print($"[WorldBridge] 完成：{steps} 步，{_world.Chronicle.Lines.Count} 条事件");

        // headless 验证用：跑完后自动退出
        GetTree().Quit();
    }
}
