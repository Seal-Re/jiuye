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
    private bool _clickable; // 当前帧是否可点击

    // 双条参数
    private float _attackerWindowStart = 0.3f;
    private float _attackerWindowEnd = 0.5f;
    private float _defenderWindowStart = 0.2f;
    private float _defenderWindowEnd = 0.45f;
    private float _pointerPos;
    private bool _attackerSuccess;
    private bool _defenderSuccess;
    private bool _playerClicked;    // 玩家是否点击了
    private bool _playerHit;        // 玩家是否点中窗口

    // 命中率追踪
    private int _totalClicks;
    private int _totalHits;

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
        _clickable = true;
        _playerClicked = false;
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
        DrawBar(20, BarY, _attackerWindowStart, _attackerWindowEnd, _attackerSuccess || _playerHit);

        // —— 防方条（下）——
        DrawString(font, new Vector2(20, BarY + BarHeight + 6), "防方 QTE",
            HorizontalAlignment.Left, -1, fontSize, new Color(0.4f, 0.6f, 0.9f));
        DrawBar(20, BarY + BarHeight + 6 + BarY, _defenderWindowStart, _defenderWindowEnd, _defenderSuccess);

        // 命中率
        string hitRate = _totalClicks > 0 ? $"命中 {_totalHits}/{_totalClicks}" : "";
        DrawString(font, new Vector2(20, BarY * 2 + BarHeight * 2 + 10), hitRate,
            HorizontalAlignment.Left, -1, 9, new Color(0.7f, 0.65f, 0.5f));

        // 点击提示
        if (_clickable && !_playerClicked)
            DrawString(font, new Vector2(20, BarY + BarHeight / 2), "← 点击窗口!",
                HorizontalAlignment.Left, -1, 10, new Color(1f, 0.9f, 0.3f, 0.8f));
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

    public override void _Input(InputEvent @event)
    {
        if (!_visible || !_clickable || _playerClicked) return;
        if (@event is not InputEventMouseButton mb || !mb.Pressed || mb.ButtonIndex != MouseButton.Left)
            return;

        var clickPos = GetGlobalMousePosition();
        var localPos = clickPos - GlobalPosition;

        float barX = 20;
        float barTop = BarY;
        float barBottom = BarY + BarHeight;
        if (localPos.X >= barX && localPos.X <= barX + BarWidth &&
            localPos.Y >= barTop - 4 && localPos.Y <= barBottom + 4)
        {
            float normX = (localPos.X - barX) / BarWidth;
            _playerHit = normX >= _attackerWindowStart && normX <= _attackerWindowEnd;
            _playerClicked = true;
            _clickable = false;
            _totalClicks++;
            if (_playerHit) _totalHits++;
            QueueRedraw();
        }
    }
}
