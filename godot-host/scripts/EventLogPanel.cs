using System.Collections.Generic;
using Godot;

namespace GodotHost;

/// <summary>
/// 古风事件滚动日志面板。订阅 WorldBridge.OnDomainEvent →
/// 卷轴风格 ScrollContainer 显示事件流。
/// 水墨配色：墨色底 + 米白字 + 朱砂高亮。
/// </summary>
public partial class EventLogPanel : Control
{
    private WorldBridge _bridge = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _list = null!;
    private RichTextLabel _headerLabel = null!;

    private const int MaxLines = 50;  // 内存中保持最多行数，超出移除旧行

    [Export]
    public int MaxVisibleLines { get; set; } = 12;

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        _bridge.OnDomainEvent += OnDomainEvent;

        // 面板底色（墨色半透明）
        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.07f, 0.10f, 0.92f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // 卷轴标题
        _headerLabel = new RichTextLabel();
        _headerLabel.BbcodeEnabled = true;
        _headerLabel.FitContent = true;
        _headerLabel.Text = "[center][color=#c8a860]　九野 · 江湖纪事　[/color][/center]";
        _headerLabel.Position = new Vector2(0, 4);
        _headerLabel.Size = new Vector2(280, 22);
        AddChild(_headerLabel);

        // 标题装饰线
        var titleLine = new ColorRect();
        titleLine.Color = new Color(0.55f, 0.42f, 0.20f, 0.6f);
        titleLine.Position = new Vector2(40, 24);
        titleLine.Size = new Vector2(200, 1);
        AddChild(titleLine);

        // 水墨边框（四个角饰——简化：四条边线）
        AddBorderLine(6, 28, 280, 168);  // 围绕滚动区域

        // 滚动容器
        _scroll = new ScrollContainer();
        _scroll.Position = new Vector2(10, 30);
        _scroll.Size = new Vector2(260, 154);
        _scroll.ScrollDeadzone = 4;

        _list = new VBoxContainer();
        _list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.AddChild(_list);
        AddChild(_scroll);

        // 面板尺寸
        SetSize(new Vector2(290, 196));
    }

    private void OnDomainEvent(string line)
    {
        var label = new RichTextLabel();
        label.BbcodeEnabled = true;
        label.FitContent = true;
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.ScrollActive = false;

        // 事件类型着色
        string color = "#c8c0a8";  // 默认 米白
        if (line.Contains("切磋")) color = "#e08060";       // 朱砂 — 战斗
        else if (line.Contains("冲破瓶颈")) color = "#c0a840"; // 金色 — 突破
        else if (line.Contains("拜入")) color = "#80b080";     // 青绿 — 入道
        else if (line.Contains("晋升") || line.Contains("攻取")) color = "#c080e0"; // 紫 — 门派
        else if (line.Contains("复仇") || line.Contains("结怨")) color = "#e06060";  // 红 — 恩怨
        else if (line.Contains("闭关")) color = "#909890";    // 灰绿 — 修炼

        label.Text = $"[color={color}]{EscapeBbcode(line)}[/color]";

        _list.AddChild(label);
        if (_list.GetChildCount() > MaxLines)
            _list.GetChild(0).QueueFree();

        // 自动滚到底
        _scroll.ScrollVertical = (int)_scroll.GetVScrollBar().MaxValue;
    }

    private static string EscapeBbcode(string s)
        => s.Replace("[", "&#91;").Replace("]", "&#93;");

    /// <summary>添加水墨风格细线边框。</summary>
    private void AddBorderLine(float x, float y, float w, float h)
    {
        var lineColor = new Color(0.45f, 0.35f, 0.18f, 0.5f);
        void AddLine(float lx, float ly, float lw, float lh)
        {
            var r = new ColorRect { Color = lineColor, Position = new Vector2(lx, ly), Size = new Vector2(lw, lh) };
            AddChild(r);
        }
        AddLine(x, y, w, 1);       // 上
        AddLine(x, y + h, w, 1);   // 下
        AddLine(x, y, 1, h);       // 左
        AddLine(x + w, y, 1, h);   // 右
    }
}
