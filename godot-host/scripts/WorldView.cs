using System.Collections.Generic;
using System.Linq;
using Godot;
using Jianghu.Model;
using Jianghu.Sim;

namespace GodotHost;

/// <summary>
/// Godot View 最小可视化闭环：TileMap 网格占位 + 角色圆点。
/// 从 WorldBridge.World 读取拓扑 + 角色 → _Draw() 渲染纯色几何。
/// 订阅 OnDomainEvent Signal → QueueRedraw 增量刷新。
/// 插值因子 InterpolationFactor 驱动角色位置平滑 lerp（不写 Core）。
/// </summary>
public partial class WorldView : Node2D
{
    private WorldBridge _bridge = null!;
    private int _nodeCount;
    private const int TileSize = 48;
    private const int TileGap = 4;
    private const int GridCols = 10;

    // 缓存：NodeId → 屏幕坐标
    private Vector2[] _nodePositions = System.Array.Empty<Vector2>();

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        _bridge.OnDomainEvent += OnDomainEvent;

        var world = _bridge.World;
        _nodeCount = world.Map?.NodeCount ?? world.Nodes.Count;

        // 计算 grid 布局
        _nodePositions = new Vector2[_nodeCount];
        for (int i = 0; i < _nodeCount; i++)
        {
            int col = i % GridCols;
            int row = i / GridCols;
            _nodePositions[i] = new Vector2(
                col * (TileSize + TileGap) + TileSize / 2f,
                row * (TileSize + TileGap) + TileSize / 2f);
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var world = _bridge.World;
        if (world == null || _nodeCount == 0) return;

        // —— 1. 网格线 ——
        var gridColor = new Color(0.15f, 0.14f, 0.18f, 1f);
        for (int i = 0; i <= _nodeCount / GridCols + 1; i++)
        {
            float y = i * (TileSize + TileGap);
            DrawLine(new Vector2(0, y), new Vector2(GridCols * (TileSize + TileGap), y), gridColor, 1);
        }
        for (int j = 0; j <= GridCols; j++)
        {
            float x = j * (TileSize + TileGap);
            DrawLine(new Vector2(x, 0), new Vector2(x, (_nodeCount / GridCols + 1) * (TileSize + TileGap)), gridColor, 1);
        }

        // —— 2. 节点 tile ——
        for (int i = 0; i < _nodeCount; i++)
        {
            var pos = _nodePositions[i];
            float half = TileSize / 2f - 2;

            // 节点底色
            Color tileColor = i == 0
                ? new Color(0.18f, 0.22f, 0.16f, 1f)  // Home node 绿色
                : new Color(0.12f, 0.11f, 0.14f, 1f);   // 普通节点 暗色

            DrawRect(new Rect2(pos.X - half, pos.Y - half, TileSize - 4, TileSize - 4), tileColor);

            // 节点 ID 标签
            DrawString(ThemeDB.FallbackFont,
                pos + new Vector2(-8, -half + 4),
                i.ToString(),
                HorizontalAlignment.Left, -1, 12,
                new Color(0.6f, 0.6f, 0.55f, 1f));
        }

        // —— 3. 角色圆点 ——
        float interp = _bridge.InterpolationFactor;
        foreach (var c in world.AliveCharacters())
        {
            int nodeIdx = c.Node.Value;
            if (nodeIdx < 0 || nodeIdx >= _nodeCount) continue;

            var pos = _nodePositions[nodeIdx];

            // 颜色按 Goal：Advance=蓝, Wander=绿, default=灰
            Color charColor = c.Goal.Kind == GoalKind.Advance
                ? new Color(0.3f, 0.55f, 0.9f, 0.9f)
                : c.Goal.Kind == GoalKind.Wander
                    ? new Color(0.3f, 0.8f, 0.4f, 0.9f)
                    : new Color(0.6f, 0.6f, 0.6f, 0.7f);

            // 圆点（插值后微调，体现动画连续性）
            float radius = 5f;
            DrawCircle(pos + new Vector2(0, -2 * interp), radius, charColor);

            // 名字缩写（2字）
            string label = c.Persona.Name.Length > 1
                ? c.Persona.Name[..2]
                : c.Persona.Name;
            DrawString(ThemeDB.FallbackFont,
                pos + new Vector2(-8, radius + 2),
                label,
                HorizontalAlignment.Left, -1, 10,
                new Color(0.85f, 0.82f, 0.7f, 1f));
        }
    }

    private void OnDomainEvent(string _) => QueueRedraw();
}
