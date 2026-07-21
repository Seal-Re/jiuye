using Godot;

namespace GodotHost;

/// <summary>
/// 伤害飘字动效——战斗事件时显示浮动数字。
/// 红字=伤害，橙字=反弹，金色=破韧。
/// 从 EventLog 解析 DuelResolved → margin→伤害量。
/// </summary>
public partial class DamagePopup : Control
{
    private WorldBridge _bridge = null!;
    private Font? _font;

    // 活跃飘字列表
    private readonly System.Collections.Generic.List<FloatingText> _active = new();

    private struct FloatingText
    {
        public string Text;
        public Color Color;
        public Vector2 Position;
        public float Age;        // 0→1 生命周期
        public float Duration;   // 总时长
    }

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        _bridge.OnDomainEvent += OnEvent;
        _font = ThemeDB.FallbackFont;
        SetSize(new Vector2(400, 300));
        Position = new Vector2(80, 100);
        MouseFilter = MouseFilterEnum.Ignore;  // 不拦截点击
    }

    private void OnEvent(string line)
    {
        if (!line.Contains("切磋")) return;

        // 解析 margin
        int margin = 0;
        int idx = line.IndexOf("差 ");
        if (idx > 0)
        {
            var sub = line[(idx + 2)..];
            int end = sub.IndexOf(' ');
            if (end < 0) end = sub.IndexOf('）');
            if (end < 0) end = sub.IndexOf(')');
            if (end > 0) int.TryParse(sub[..end], out margin);
        }

        // 攻方伤害 = 100 - margin，防方伤害 = margin
        int atkDmg = System.Math.Max(5, 100 - margin);
        int defDmg = System.Math.Max(5, margin);

        // 飘字：攻方伤害（红色），防方伤害（橙色），破韧（金色 if margin>50）
        Spawn($"-{atkDmg}", new Color(1f, 0.3f, 0.2f), new Vector2(100, 160));
        Spawn($"-{defDmg}", new Color(1f, 0.55f, 0.2f), new Vector2(180, 160));
        if (margin > 50)
            Spawn("破韧!", new Color(1f, 0.85f, 0.2f), new Vector2(140, 130));

        QueueRedraw();
    }

    private void Spawn(string text, Color color, Vector2 pos)
    {
        _active.Add(new FloatingText
        {
            Text = text, Color = color, Position = pos,
            Age = 0f, Duration = 1.2f
        });
    }

    public override void _Process(double delta)
    {
        if (_active.Count == 0) return;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ft = _active[i];
            ft.Age += (float)delta / ft.Duration;
            if (ft.Age >= 1f)
                _active.RemoveAt(i);
            else
                _active[i] = ft;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_font == null) return;
        foreach (var ft in _active)
        {
            // 上浮 + 渐隐
            float alpha = 1f - ft.Age;
            float yOffset = ft.Age * 30f;
            var pos = ft.Position + new Vector2(0, -yOffset);
            var color = new Color(ft.Color.R, ft.Color.G, ft.Color.B, alpha);

            // 阴影
            DrawString(_font, pos + new Vector2(1, 1), ft.Text,
                HorizontalAlignment.Center, -1, 16, new Color(0, 0, 0, alpha * 0.5f));
            // 主文字
            DrawString(_font, pos, ft.Text,
                HorizontalAlignment.Center, -1, 16, color);
        }
    }
}
