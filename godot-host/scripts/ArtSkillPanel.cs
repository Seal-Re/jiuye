using System.Collections.Generic;
using Godot;
using Jianghu.Cultivation;

namespace GodotHost;

/// <summary>
/// 功法/战技管理面板——Play P2。
/// 显示玩家已学功法(ChosenArtIds)+已装备战技(ChosenSkillIds)。
/// 点击可查看详情。
/// </summary>
public partial class ArtSkillPanel : Control
{
    private WorldBridge _bridge = null!;
    private VBoxContainer _artList = null!;
    private VBoxContainer _skillList = null!;
    private RichTextLabel _infoLabel = null!;

    private CultivationPathDef? _playerPath;
    private PathRegistry? _registry;

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        _registry = new PathRegistry(new CodePathSource());

        // 面板底色
        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.07f, 0.09f, 0.9f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // 标题
        var title = new RichTextLabel();
        title.BbcodeEnabled = true;
        title.FitContent = true;
        title.Text = "[center][color=#c8a860]　功法 · 战技　[/color][/center]";
        title.Position = new Vector2(0, 4);
        title.Size = new Vector2(220, 20);
        AddChild(title);

        // 功法列表（左）
        var artLabel = new Label();
        artLabel.Text = "功法";
        artLabel.Position = new Vector2(8, 28);
        artLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.65f, 0.5f));
        AddChild(artLabel);

        var artScroll = new ScrollContainer();
        artScroll.Position = new Vector2(8, 46);
        artScroll.Size = new Vector2(102, 160);
        _artList = new VBoxContainer();
        artScroll.AddChild(_artList);
        AddChild(artScroll);

        // 战技列表（右）
        var skillLabel = new Label();
        skillLabel.Text = "战技";
        skillLabel.Position = new Vector2(116, 28);
        skillLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.65f, 0.5f));
        AddChild(skillLabel);

        var skillScroll = new ScrollContainer();
        skillScroll.Position = new Vector2(116, 46);
        skillScroll.Size = new Vector2(98, 160);
        _skillList = new VBoxContainer();
        skillScroll.AddChild(_skillList);
        AddChild(skillScroll);

        // 详情
        _infoLabel = new RichTextLabel();
        _infoLabel.BbcodeEnabled = true;
        _infoLabel.FitContent = true;
        _infoLabel.Position = new Vector2(8, 212);
        _infoLabel.Size = new Vector2(206, 40);
        AddChild(_infoLabel);

        SetSize(new Vector2(222, 256));
        Position = new Vector2(540, 260);

        CallDeferred(nameof(Refresh));
    }

    public void Refresh()
    {
        foreach (var c in _artList.GetChildren()) c.QueueFree();
        foreach (var c in _skillList.GetChildren()) c.QueueFree();

        var player = _bridge.PlayerCharacter;
        if (player?.Cultivation == null || _registry == null) return;

        var st = player.Cultivation;
        var def = _registry.ById(st.PathId);
        _playerPath = def;

        // 功法列表
        foreach (var artId in st.ChosenArtIds)
        {
            var btn = MakeItemBtn(artId, true);
            _artList.AddChild(btn);
        }

        // 战技列表
        foreach (var skId in st.ChosenSkillIds)
        {
            var btn = MakeItemBtn(skId, false);
            _skillList.AddChild(btn);
        }
    }

    private Button MakeItemBtn(string id, bool isArt)
    {
        var btn = new Button();
        btn.Text = id.Length > 12 ? id[..12] : id;
        btn.Flat = true;
        btn.Alignment = HorizontalAlignment.Left;
        btn.AddThemeColorOverride("font_color", isArt
            ? new Color(0.5f, 0.7f, 0.5f) : new Color(0.7f, 0.55f, 0.3f));
        btn.AddThemeFontSizeOverride("font_size", 10);
        btn.CustomMinimumSize = new Vector2(0, 18);

        string captureId = id;
        btn.Pressed += () => ShowDetail(captureId, isArt);
        return btn;
    }

    private void ShowDetail(string id, bool isArt)
    {
        string detail = isArt ? FindArtDetail(id) : FindSkillDetail(id);
        _infoLabel.Text = $"[color=#b0a080]{id}[/color]\n[color=#888]{detail}[/color]";
    }

    private string FindArtDetail(string id)
    {
        if (_playerPath == null) return "";
        foreach (var cat in _playerPath.ArtCategories)
            foreach (var art in cat.Arts)
                if (art.Id == id) return $"{cat.Name} · T{art.Tier}";
        return "未知功法";
    }

    private string FindSkillDetail(string id)
    {
        if (_playerPath == null) return "";
        foreach (var sk in _playerPath.CombatSkills)
            if (sk.Id == id) return $"T{sk.Tier} · {sk.Damage}伤";
        return "未知战技";
    }
}
