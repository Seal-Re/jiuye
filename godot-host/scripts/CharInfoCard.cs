using Godot;
using Jianghu.Cultivation;
using Jianghu.Model;

namespace GodotHost;

/// <summary>
/// 卷轴风格角色信息卡。点击角色图标时弹出。
/// 显示：姓名/称号/身份 → 四维 → 路径/境界 → 资源 → 功法/战技。
/// </summary>
public partial class CharInfoCard : Control
{
    private RichTextLabel _content = null!;
    private Button _closeBtn = null!;
    private Panel _panel = null!;

    public override void _Ready()
    {
        // 面板容器
        _panel = new Panel();
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_panel);

        // 卷轴底色
        var bg = new StyleBoxFlat();
        bg.BgColor = new Color(0.12f, 0.10f, 0.08f, 0.96f);
        bg.BorderWidthLeft = bg.BorderWidthRight = 4;
        bg.BorderWidthTop = bg.BorderWidthBottom = 4;
        bg.BorderColor = new Color(0.55f, 0.42f, 0.20f, 1f);  // 金色边框
        bg.CornerRadiusTopLeft = bg.CornerRadiusTopRight = 6;
        bg.CornerRadiusBottomLeft = bg.CornerRadiusBottomRight = 6;
        _panel.AddThemeStyleboxOverride("panel", bg);

        // 关闭按钮（右上角朱砂×）
        _closeBtn = new Button();
        _closeBtn.Text = "✕";
        _closeBtn.Position = new Vector2(220, 6);
        _closeBtn.Size = new Vector2(24, 24);
        _closeBtn.AddThemeColorOverride("font_color", new Color(0.85f, 0.35f, 0.30f));
        _closeBtn.AddThemeFontSizeOverride("font_size", 14);
        _closeBtn.Pressed += Hide;
        AddChild(_closeBtn);

        // 内容区域
        _content = new RichTextLabel();
        _content.BbcodeEnabled = true;
        _content.FitContent = true;
        _content.Position = new Vector2(12, 34);
        _content.Size = new Vector2(220, 200);
        _content.ScrollActive = false;
        AddChild(_content);

        SetSize(new Vector2(248, 260));
        Hide();
    }

    public void ShowCharacter(Character c, WorldBridge bridge)
    {
        var st = c.Cultivation;
        var registry = new PathRegistry(new CodePathSource());
        var def = st != null ? registry.ById(st.PathId) : null;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[center][color=#c8a860]{c.Persona.Name} · {c.Persona.Title}[/color][/center]");
        sb.AppendLine($"[color=#888]　{c.Persona.Origin} · {c.Persona.Archetype}[/color]");
        sb.AppendLine("");

        // 四维
        sb.Append("[color=#b8b0a0]");
        sb.Append($"武{c.Stats.Get(Jianghu.Stats.StatKind.Force)} ");
        sb.Append($"内{c.Stats.Get(Jianghu.Stats.StatKind.Internal)} ");
        sb.Append($"根{c.Stats.Get(Jianghu.Stats.StatKind.Constitution)} ");
        sb.Append($"悟{c.Stats.Get(Jianghu.Stats.StatKind.Insight)}");
        sb.AppendLine("[/color]");

        // 路径 + 境界
        if (def != null && st != null)
        {
            string realmName = st.RealmIndex < def.Curve.RealmNames.Count
                ? def.Curve.RealmNames[st.RealmIndex] : $"第{st.RealmIndex}重";
            int pe = PowerEngine.Evaluate(st, c.Stats, def, bridge.World.Limits);
            sb.AppendLine($"[color=#c0a840]{def.Name}[/color] · {realmName}");
            sb.AppendLine($"[color=#909890]战力 {pe} | CP {st.CultivationPoints}[/color]");

            // 资源
            if (st.Resources.Count > 0)
            {
                sb.Append("[color=#8090a0]");
                int count = 0;
                foreach (var kv in st.Resources)
                {
                    if (kv.Value != 0 && count < 4)
                    {
                        sb.Append($"{kv.Key}:{kv.Value} ");
                        count++;
                    }
                }
                sb.AppendLine("[/color]");
            }

            // 功法 (前4)
            if (st.ChosenArtIds.Count > 0)
            {
                sb.Append("[color=#708070]功: ");
                for (int i = 0; i < st.ChosenArtIds.Count && i < 4; i++)
                    sb.Append($"{st.ChosenArtIds[i]} ");
                sb.AppendLine("[/color]");
            }

            // 战技 (前3)
            if (st.ChosenSkillIds.Count > 0)
            {
                sb.Append("[color=#907060]技: ");
                for (int i = 0; i < st.ChosenSkillIds.Count && i < 3; i++)
                    sb.Append($"{st.ChosenSkillIds[i]} ");
                sb.AppendLine("[/color]");
            }
        }
        else
        {
            sb.AppendLine("[color=#888]未入道[/color]");
        }

        // 寿元 + 年龄
        sb.AppendLine($"[color=#686860]寿{c.Lifespan - c.Age}/{c.Lifespan} | 龄{c.Age}[/color]");

        _content.Text = sb.ToString();
        Show();
    }
}
