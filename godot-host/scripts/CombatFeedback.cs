using Godot;

namespace GodotHost;

/// <summary>
/// 战斗反馈特效——命中闪光/格挡闪烁/破韧震屏。
/// 叠加在 WorldView 之上，由 DomainEvent 中的切磋事件触发。
/// </summary>
public partial class CombatFeedback : Control
{
    private WorldBridge _bridge = null!;
    private float _flashAlpha;       // 闪光透明度 0→1→0
    private float _shakeOffset;      // 震屏偏移
    private float _shakeDecay = 0.9f;
    private Color _flashColor = Colors.White;

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        _bridge.OnDomainEvent += OnEvent;
        MouseFilter = MouseFilterEnum.Ignore;
        SetSize(new Vector2(600, 500));
    }

    private void OnEvent(string line)
    {
        if (line.Contains("切磋"))
        {
            // 命中闪光
            _flashAlpha = 0.5f;
            _flashColor = line.Contains("胜") ? new Color(1f, 0.8f, 0.2f, 1f) : new Color(1f, 0.3f, 0.2f, 1f);

            // 破韧震屏（margin>50 表示破韧）
            if (line.Contains("差 ") && int.TryParse(line.Split("差 ")[1].Split(')')[0], out int m) && m > 50)
                _shakeOffset = 6f;

            QueueRedraw();
        }
    }

    public override void _Process(double delta)
    {
        bool needsRedraw = false;

        if (_flashAlpha > 0.01f)
        {
            _flashAlpha *= 0.85f;
            needsRedraw = true;
        }
        else _flashAlpha = 0f;

        if (System.Math.Abs(_shakeOffset) > 0.1f)
        {
            _shakeOffset *= -_shakeDecay;
            needsRedraw = true;
        }
        else _shakeOffset = 0f;

        if (needsRedraw) QueueRedraw();
    }

    public override void _Draw()
    {
        if (_flashAlpha > 0.01f)
        {
            var flashRect = new Rect2(0, 0, 600, 500);
            DrawRect(flashRect, new Color(_flashColor.R, _flashColor.G, _flashColor.B, _flashAlpha));
        }

        if (System.Math.Abs(_shakeOffset) > 0.1f)
        {
            // 震屏通过位移 WorldView 实现——这里绘制一个指示
            DrawString(ThemeDB.FallbackFont, new Vector2(200 + _shakeOffset, 10),
                "BREAK!", HorizontalAlignment.Left, -1, 14, new Color(1f, 0.8f, 0.2f));
        }
    }
}
