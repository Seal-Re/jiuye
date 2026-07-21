using System.Collections.Generic;
using Godot;
using Jianghu.Model;
using Jianghu.Sim;

namespace GodotHost;

/// <summary>
/// Godot View 层渲染：TileMap 网格 + 21路像素角色 Sprite。
/// 从 WorldBridge.World 读取拓扑 + 角色 → _Draw() 渲染纯色几何。
/// 订阅 OnDomainEvent Signal → QueueRedraw 增量刷新。
/// </summary>
public partial class WorldView : Node2D
{
    private WorldBridge _bridge = null!;
    private int _nodeCount;
    private const int TileSize = 48;
    private const int TileGap = 4;
    private const int GridCols = 10;
    private const int IconSize = 24;  // display size for character icons

    private Vector2[] _nodePositions = System.Array.Empty<Vector2>();
    private Texture2D? _spriteSheet;
    private readonly Dictionary<string, Rect2> _pathIconRects = new();

    // icon_gen.py PATHS 顺序 (7 cols × 3 rows)
    private static readonly string[] PathOrder =
    {
        "sword_immortal", "ti_xiu_hengshi", "fa_xiu", "gui_xiu_yang_hun",
        "dan_xiu", "qixiu_artificer", "array_formation", "soul_divine_sense",
        "lei_xiu", "buddhist_golden_body", "ming_fate_causality", "yu_shou",
        "ru_xiu_haoran", "mo_xiu_xinmo", "yao_xiu_huaxing", "xue_xiu_xuesha",
        "du_gu_xiu", "fu_xiu_fulu", "kuilei_shi", "yin_xiu_yuedao", "yinguo_faze"
    };

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        _bridge.OnDomainEvent += OnDomainEvent;

        var world = _bridge.World;
        _nodeCount = world.Map?.NodeCount ?? world.Nodes.Count;

        // grid 布局
        _nodePositions = new Vector2[_nodeCount];
        for (int i = 0; i < _nodeCount; i++)
        {
            int col = i % GridCols;
            int row = i / GridCols;
            _nodePositions[i] = new Vector2(
                col * (TileSize + TileGap) + TileSize / 2f,
                row * (TileSize + TileGap) + TileSize / 2f);
        }

        // 加载 sprite sheet（从文件字节创建 ImageTexture，绕过 Godot import 管线）
        var img = new Image();
        var err = img.Load("res://assets/path_icons.png");
        if (err == Error.Ok)
        {
            _spriteSheet = ImageTexture.CreateFromImage(img);

            const int cols = 7;
            const int cell = 192;  // 48*4 native → display scale
            const int pad = 10;
            for (int i = 0; i < PathOrder.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x = pad + col * (cell + pad);
                float y = 36 + row * (cell + 20 + pad);  // 36 = title offset, 20 = label height
                _pathIconRects[PathOrder[i]] = new Rect2(x, y, cell, cell);
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var world = _bridge.World;
        if (world == null || _nodeCount == 0) return;

        // —— 网格背景 ——
        var gridColor = new Color(0.12f, 0.11f, 0.14f, 1f);
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

        // —— 节点 tile + ID ——
        for (int i = 0; i < _nodeCount; i++)
        {
            var pos = _nodePositions[i];
            float half = TileSize / 2f - 2;
            Color tileColor = i == 0
                ? new Color(0.15f, 0.20f, 0.14f, 1f)
                : new Color(0.10f, 0.09f, 0.12f, 1f);
            DrawRect(new Rect2(pos.X - half, pos.Y - half, TileSize - 4, TileSize - 4), tileColor);
            DrawString(ThemeDB.FallbackFont, pos + new Vector2(-6, -half + 2), i.ToString(),
                HorizontalAlignment.Left, -1, 10, new Color(0.5f, 0.5f, 0.45f, 1f));
        }

        // —— 角色 Sprite 图标 ——
        float interp = _bridge.InterpolationFactor;
        foreach (var c in world.AliveCharacters())
        {
            int nodeIdx = c.Node.Value;
            if (nodeIdx < 0 || nodeIdx >= _nodeCount) continue;
            var pos = _nodePositions[nodeIdx] + new Vector2(0, -2 * interp);

            string pathId = c.Cultivation?.PathId ?? "";
            if (_spriteSheet != null && _pathIconRects.TryGetValue(pathId, out var srcRect))
            {
                var dstRect = new Rect2(pos.X - IconSize / 2f, pos.Y - IconSize / 2f, IconSize, IconSize);
                DrawTextureRectRegion(_spriteSheet, dstRect, srcRect);
            }
            else
            {
                // fallback: 彩色圆点
                Color fallback = new Color(0.4f, 0.5f, 0.9f, 0.8f);
                DrawCircle(pos, 5f, fallback);
            }

            // 名字标签
            string label = c.Persona.Name.Length > 1 ? c.Persona.Name[..2] : c.Persona.Name;
            DrawString(ThemeDB.FallbackFont, pos + new Vector2(-8, IconSize / 2f + 2), label,
                HorizontalAlignment.Left, -1, 9, new Color(0.8f, 0.78f, 0.65f, 1f));
        }
    }

    private void OnDomainEvent(string _) => QueueRedraw();
}
