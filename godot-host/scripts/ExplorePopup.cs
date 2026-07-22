using Godot;

namespace GodotHost;

/// <summary>
/// 探索秘境面板——点击节点时概率触发秘境发现。
/// 秘境：随机资源/功法/战力增益。
/// </summary>
public partial class ExplorePopup : Control
{
    private WorldBridge _bridge = null!;
    private RichTextLabel _resultLabel = null!;
    private Button _closeBtn = null!;

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.07f, 0.12f, 0.93f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var title = new RichTextLabel();
        title.BbcodeEnabled = true;
        title.FitContent = true;
        title.Text = "[center][color=#80c0a0]　秘境探索　[/color][/center]";
        title.Position = new Vector2(0, 8);
        title.Size = new Vector2(240, 20);
        AddChild(title);

        _resultLabel = new RichTextLabel();
        _resultLabel.BbcodeEnabled = true;
        _resultLabel.FitContent = true;
        _resultLabel.Position = new Vector2(16, 36);
        _resultLabel.Size = new Vector2(210, 80);
        AddChild(_resultLabel);

        _closeBtn = new Button();
        _closeBtn.Text = "继续前行";
        _closeBtn.Position = new Vector2(70, 120);
        _closeBtn.Size = new Vector2(100, 28);
        _closeBtn.Pressed += Hide;
        AddChild(_closeBtn);

        SetSize(new Vector2(240, 160));
        Position = new Vector2(280, 180);
        Hide();
    }

    public void Explore(int nodeId)
    {
        if (_bridge?.World == null) return;

        // 秘境概率：30% 触发
        float roll = GD.Randf();
        if (roll > 0.3f)
        {
            _resultLabel.Text = "[color=#888]此地平淡无奇...\n继续前行吧。[/color]";
            Show();
            return;
        }

        // 随机发现
        int discoveryType = (int)(GD.Randi() % 4);
        string result = discoveryType switch
        {
            0 => "[color=#c8a860]发现前人洞府！\n获得功法残卷 +50 CP[/color]",
            1 => "[color=#80c0a0]灵气充沛的灵泉！\n灵气恢复 +30[/color]",
            2 => "[color=#c080e0]遭遇妖兽巢穴！\n战斗后获得兽骨 +20 CP[/color]",
            _ => "[color=#e0a060]发现古传送阵！\n随机传送到另一节点[/color]"
        };
        _resultLabel.Text = result;

        if (_bridge.PlayerCharacter?.Cultivation != null)
            _bridge.PlayerCharacter.Cultivation.CultivationPoints += 30 + (int)(GD.Randi() % 40);

        Show();
    }
}
