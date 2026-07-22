using System.Linq;
using Godot;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Sim;

namespace GodotHost;

/// <summary>
/// 角色创建面板——Play-Alpha-1 P0。
/// 选名/选路 → WorldFactory.CreateTypicalChar → 注入 World。
/// </summary>
public partial class CharCreatePanel : Control
{
    private WorldBridge _bridge = null!;
    private OptionButton _pathSelect = null!;
    private LineEdit _nameInput = null!;
    private Button _createBtn = null!;
    private RichTextLabel _previewLabel = null!;

    private CultivationPathDef[] _paths = System.Array.Empty<CultivationPathDef>();
    private bool _panelVisible = true;

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");

        // 面板底色
        var bg = new ColorRect();
        bg.Color = new Color(0.10f, 0.09f, 0.07f, 0.95f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // 金色边框
        var panel = new Panel();
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0, 0, 0, 0);
        style.BorderWidthLeft = style.BorderWidthRight = 2;
        style.BorderWidthTop = style.BorderWidthBottom = 2;
        style.BorderColor = new Color(0.55f, 0.42f, 0.20f, 0.8f);
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        // 标题
        var title = new RichTextLabel();
        title.BbcodeEnabled = true;
        title.FitContent = true;
        title.Text = "[center][color=#c8a860]　踏入江湖　[/color][/center]";
        title.Position = new Vector2(0, 12);
        title.Size = new Vector2(260, 24);
        AddChild(title);

        // 名字输入
        var nameLabel = new Label();
        nameLabel.Text = "名号";
        nameLabel.Position = new Vector2(16, 48);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.75f, 0.6f));
        AddChild(nameLabel);

        _nameInput = new LineEdit();
        _nameInput.Position = new Vector2(60, 46);
        _nameInput.Size = new Vector2(120, 24);
        _nameInput.Text = "无名";
        _nameInput.TextChanged += (_) => UpdatePreview();
        AddChild(_nameInput);

        // 路径选择
        var pathLabel = new Label();
        pathLabel.Text = "修炼路";
        pathLabel.Position = new Vector2(16, 80);
        pathLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.75f, 0.6f));
        AddChild(pathLabel);

        _pathSelect = new OptionButton();
        _pathSelect.Position = new Vector2(60, 78);
        _pathSelect.Size = new Vector2(180, 24);
        var src = new CodePathSource();
        _paths = src.Load().ToArray();
        for (int i = 0; i < _paths.Length; i++)
            _pathSelect.AddItem($"{_paths[i].Name} ({_paths[i].PathId})", i);
        _pathSelect.ItemSelected += (_) => UpdatePreview();
        AddChild(_pathSelect);

        // 预览
        _previewLabel = new RichTextLabel();
        _previewLabel.BbcodeEnabled = true;
        _previewLabel.FitContent = true;
        _previewLabel.Position = new Vector2(16, 112);
        _previewLabel.Size = new Vector2(228, 60);
        AddChild(_previewLabel);

        // 创建按钮
        _createBtn = new Button();
        _createBtn.Text = "踏入江湖";
        _createBtn.Position = new Vector2(70, 180);
        _createBtn.Size = new Vector2(120, 32);
        _createBtn.AddThemeColorOverride("font_color", new Color(0.9f, 0.8f, 0.5f));
        _createBtn.Pressed += OnCreate;
        AddChild(_createBtn);

        SetSize(new Vector2(260, 224));
        Position = new Vector2(270, 150);

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        int idx = _pathSelect.Selected;
        if (idx < 0 || idx >= _paths.Length) return;
        var p = _paths[idx];
        _previewLabel.Text =
            $"[color=#b0a888]路:[/color] [color=#c8a860]{p.Name}[/color]\n" +
            $"[color=#b0a888]攻:[/color] {p.AttackDimension}\n" +
            $"[color=#b0a888]tag:[/color] {string.Join(", ", p.SituationalTags.Take(3))}";
    }

    private void OnCreate()
    {
        int idx = _pathSelect.Selected;
        if (idx < 0 || idx >= _paths.Length) return;

        string name = string.IsNullOrWhiteSpace(_nameInput.Text) ? "无名" : _nameInput.Text.Trim();
        var pathDef = _paths[idx];

        // 用 WorldFactory 创建典型角色
        var ch = WorldFactory.CreateTypicalChar(pathDef.PathId, ut: 2, pathDef, id: 999);
        ch.Persona = new Persona(name, "散客", "市井", ArchetypeKind.Martial, null);
        ch.Node = new NodeId(0);  // 出生在 Home 节点

        // 注入 World（通过 WorldBridge）
        _bridge.InjectPlayerCharacter(ch, pathDef);

        GD.Print($"[CharCreate] {name} 踏入江湖——{pathDef.Name}({pathDef.PathId})");
        Hide();
    }
}
