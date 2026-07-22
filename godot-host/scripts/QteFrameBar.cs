using Godot;

namespace GodotHost;

/// <summary>
/// QTE 防御帧窗进度条——战斗时角色头顶显示 FrameHook 可视化。
/// 双条布局：攻方（上）防方（下），成功窗高亮，指针扫过。
///
/// 当前为视觉原型：数据来自 DuelResolved 事件的 Margin→窗口估算。
/// FrameHook 实时数据接入需 Core 侧暴露（后续 story），届时替换估算逻辑。
/// </summary>
public partial class QteFrameBar : Control
{
    private WorldBridge _bridge = null!;
    private bool _visible;

    // 双条参数
    private float _attackerWindowStart = 0.3f;   // 归一化 [0,1]
    private float _attackerWindowEnd = 0.5f;
    private float _defenderWindowStart = 0.2f;
    private float _defenderWindowEnd = 0.45f;
    private float _pointerPos;                     // 动画指针位置
    private bool _attackerSuccess;
    private bool _defenderSuccess;

    private Tween? _tween;
    private int _lastDuelCount;

    private const float BarWidth = 180;
    private const float BarHeight = 12f;
    private const float BarY = 20;

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        _bridge.OnDomainEvent += OnEvent;
        SetSize(new Vector2(BarWidth + 40, 80));
        Position = new Vector2(300, 4);
        Hide();
    }

    private void OnEvent(string line)
    {
        if (!line.Contains("切磋")) return;

        // 估算窗口大小：margin 越小→窗口越窄（更难）
        int marginIdx = line.IndexOf("差 ");
        int margin = 0;
        if (marginIdx > 0)
        {
            var sub = line[(marginIdx + 2)..];
            var end = sub.IndexOf(' ');
            if (end < 0) end = sub.IndexOf('）');
            if (end < 0) end = sub.IndexOf(')');
            if (end > 0) int.TryParse(sub[..end], out margin);
        }

        float difficulty = System.Math.Clamp(margin / 100f, 0.05f, 0.6f);
        float windowWidth = 0.5f - difficulty;
        float rng = GD.Randf();
        _attackerWindowStart = rng * (1f - windowWidth);
        _attackerWindowEnd = _attackerWindowStart + windowWidth;
        rng = GD.Randf();
        _defenderWindowStart = rng * (1f - windowWidth);
        _defenderWindowEnd = _defenderWindowStart + windowWidth;

        // 模拟判定：margin<50→攻方"点中"，margin>50→防方"点中"
        _attackerSuccess = margin < 50;
        _defenderSuccess = margin >= 50;

        _pointerPos = 0f;
        Show();
        _visible = true;

        // 动画：指针扫过 0→1（0.6s），然后自动隐藏
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(this, "_pointerPos", 1.0f, 0.6f);
        _tween.TweenCallback(Callable.From(() =>
        {
            _visible = false;
            Hide();
        }));
        _tween.TweenInterval(0.5f);  // 短暂停留
    }

    public override void _Draw()
    {
        if (!_visible) return;

        var font = ThemeDB.FallbackFont;
        int fontSize = 11;

        // —— 攻方条（上）——
        DrawString(font, new Vector2(20, 0), "攻方 QTE", HorizontalAlignment.Left, -1, fontSize, new Color(0.9f, 0.5f, 0.3f));
        DrawBar(20, BarY, _attackerWindowStart, _attackerWindowEnd, _attackerSuccess);

        // —— 防方条（下）——
        DrawString(font, new Vector2(20, BarY + BarHeight + 6), "防方 QTE",
            HorizontalAlignment.Left, -1, fontSize, new Color(0.4f, 0.6f, 0.9f));
        DrawBar(20, BarY + BarHeight + 6 + BarY, _defenderWindowStart, _defenderWindowEnd, _defenderSuccess);
    }

    private void DrawBar(float x, float y, float winStart, float winEnd, bool success)
    {
        // 背景条
        var bgColor = new Color(0.15f, 0.13f, 0.18f, 0.9f);
        DrawRect(new Rect2(x, y, BarWidth, BarHeight), bgColor);

        // 成功窗（绿色/金色）
        float winX = x + winStart * BarWidth;
        float winW = (winEnd - winStart) * BarWidth;
        var winColor = success
            ? new Color(0.3f, 0.8f, 0.3f, 0.7f)
            : new Color(0.5f, 0.4f, 0.1f, 0.5f);
        DrawRect(new Rect2(winX, y, winW, BarHeight), winColor);

        // 窗口边界线
        var edgeColor = new Color(0.9f, 0.75f, 0.2f, 0.8f);
        DrawLine(new Vector2(winX, y), new Vector2(winX, y + BarHeight), edgeColor);
        DrawLine(new Vector2(winX + winW, y), new Vector2(winX + winW, y + BarHeight), edgeColor);

        // 指针
        float px = x + _pointerPos * BarWidth;
        var pointerColor = success ? new Color(0.2f, 1f, 0.3f, 0.9f) : new Color(1f, 0.3f, 0.2f, 0.9f);
        DrawLine(new Vector2(px, y - 2), new Vector2(px, y + BarHeight + 2), pointerColor, 2);
        DrawCircle(new Vector2(px, y + BarHeight / 2f), 3f, pointerColor);

        // 判定结果
        string result = success ? "✓ HIT" : "✗ MISS";
        var resultColor = success ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.2f);
        DrawString(ThemeDB.FallbackFont, new Vector2(x + BarWidth + 6, y), result,
            HorizontalAlignment.Left, -1, 10, resultColor);
    }
}
