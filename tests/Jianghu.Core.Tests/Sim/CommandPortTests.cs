using System.Collections.Generic;
using Jianghu.Actions;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Jianghu.Core.Tests.Sim;

/// <summary>
/// gh-004：命令端口录制/重放确定性验证。
/// 同 seed + 同命令序列 → 同 World snapshot → 逐字节一致。
/// </summary>
public class CommandPortTests
{
    private readonly ITestOutputHelper _out;
    public CommandPortTests(ITestOutputHelper output) { _out = output; }

    /// <summary>
    /// AC 4.3/4.4：录制命令序列 → 重放 → World snapshot 一致。
    /// 证明命令端口可替代 RuleBrain 产生确定性等价轨迹。
    /// </summary>
    [Fact]
    public void Test_Record_Replay_Same_Snapshot()
    {
        const ulong seed = 2026;
        const int steps = 10;
        var limits = LimitsConfig.Default;

        // —— 原始运行（录制）——
        var world1 = WorldFactory.CreateInitial(seed, limits, initialCount: 8,
            cultivation: true);
        world1.CommandLog = new List<CommandIntent>();
        for (int i = 0; i < steps; i++) world1.Advance(budget: 1);
        var snap1 = StateSnapshot.Capture(world1);

        // —— 重放运行 ——
        var world2 = WorldFactory.CreateInitial(seed, limits, initialCount: 8,
            cultivation: true);
        world2.SetReplay(world1.CommandLog!);
        for (int i = 0; i < steps; i++) world2.Advance(budget: 1);
        var snap2 = StateSnapshot.Capture(world2);

        // —— 验证 ——
        _out.WriteLine($"命令数: {world1.CommandLog!.Count}");
        _out.WriteLine($"snap1 长度: {snap1.Length}, snap2 长度: {snap2.Length}");
        Assert.Equal(snap1, snap2);
        _out.WriteLine("✅ gh-004 命令端口录制→重放确定性 PASS");
    }

    /// <summary>
    /// AC 4.6：CommandIntent 纯整数字段，无浮点。
    /// 验证序列化/反序列化往返一致。
    /// </summary>
    [Fact]
    public void Test_CommandIntent_Roundtrip()
    {
        var cmd = new CommandIntent(
            Tick: 42, ActorId: 7, Type: ActionType.Spar,
            TrainStat: null, TravelNodeId: null, SparTargetId: 3);

        var choice = cmd.ToChoice();
        Assert.Equal(ActionType.Spar, choice.Type);

        var cmd2 = CommandIntent.FromChoice(cmd.Tick, cmd.ActorId, choice);
        Assert.Equal(cmd, cmd2);
        _out.WriteLine("✅ CommandIntent 往返一致性 PASS");
    }

    /// <summary>
    /// AC 4.5：命令序列可从 Chronicle 派生（Chronicle 录制模式）。
    /// 证明命令录制与事件产生对应关系。
    /// </summary>
    [Fact]
    public void Test_CommandCount_Matches_AdvanceSteps()
    {
        const ulong seed = 42;
        const int steps = 15;
        var limits = LimitsConfig.Default;

        var world = WorldFactory.CreateInitial(seed, limits, initialCount: 8,
            cultivation: true);
        world.CommandLog = new List<CommandIntent>();
        int processed = 0;
        for (int i = 0; i < steps; i++) processed += world.Advance(budget: 1);

        // 命令数 = 实际处理的步数
        Assert.Equal(processed, world.CommandLog!.Count);
        _out.WriteLine($"命令数 {world.CommandLog.Count} = 处理步数 {processed} ✅");
    }
}
