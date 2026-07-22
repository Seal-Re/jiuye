using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using Jianghu.Actions;
using Jianghu.Sim;

namespace GodotHost;

/// <summary>
/// 存档/读档管理——Play P3。
/// 录制命令序列 + StateSnapshot → JSON 存档。
/// 加载时从存档恢复 World 状态（WorldFactory + SetReplay 重放）。
/// </summary>
public partial class SaveLoadManager : Node
{
    private WorldBridge _bridge = null!;
    private const string SaveDir = "user://saves/";

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        DirAccess.MakeDirAbsolute(SaveDir);
    }

    /// <summary>存档：录制当前状态 + 命令序列 → JSON。</summary>
    public void Save(string slotName)
    {
        var world = _bridge.World;
        if (world?.CommandLog == null) return;

        var saveData = new SaveData
        {
            Seed = 42, // TODO: 从 WorldBridge 读取实际 seed
            StepCount = world.Chronicle.Lines.Count,
            Snapshot = StateSnapshot.Capture(world),
            Commands = new List<CommandIntent>(world.CommandLog),
            SavedAt = System.DateTime.Now.ToString("O")
        };

        var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(SaveDir, $"{slotName}.json");
        File.WriteAllText(ProjectSettings.GlobalizePath(path), json);
        GD.Print($"[SaveLoad] 已存档: {slotName} ({saveData.Commands.Count} 条命令)");
    }

    /// <summary>读档：从 JSON 恢复（重新创建 World + 重放命令序列）。</summary>
    public SaveData? Load(string slotName)
    {
        var path = Path.Combine(SaveDir, $"{slotName}.json");
        var fullPath = ProjectSettings.GlobalizePath(path);
        if (!File.Exists(fullPath))
        {
            GD.PrintErr($"[SaveLoad] 存档不存在: {slotName}");
            return null;
        }

        var json = File.ReadAllText(fullPath);
        var data = JsonSerializer.Deserialize<SaveData>(json);
        GD.Print($"[SaveLoad] 已读档: {slotName} ({data?.Commands.Count ?? 0} 条命令)");
        return data;
    }

    /// <summary>列出所有存档。</summary>
    public string[] ListSaves()
    {
        var dir = ProjectSettings.GlobalizePath(SaveDir);
        if (!Directory.Exists(dir)) return System.Array.Empty<string>();
        var files = Directory.GetFiles(dir, "*.json");
        var names = new string[files.Length];
        for (int i = 0; i < files.Length; i++)
            names[i] = Path.GetFileNameWithoutExtension(files[i]);
        return names;
    }

    public class SaveData
    {
        public ulong Seed { get; set; }
        public int StepCount { get; set; }
        public string Snapshot { get; set; } = "";
        public List<CommandIntent> Commands { get; set; } = new();
        public string SavedAt { get; set; } = "";
    }
}
