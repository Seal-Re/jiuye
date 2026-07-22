using Godot;

namespace GodotHost;

/// <summary>
/// 意图操作栏——Play-Alpha-1 P0。
/// 修炼/游历/切磋 三按钮 + 暂停/继续。
/// 选目标（点击世界）后启用对应按钮。
/// </summary>
public partial class ActionBar : Control
{
    private WorldBridge _bridge = null!;
    private WorldView _worldView = null!;
    private Button _trainBtn = null!;
    private Button _travelBtn = null!;
    private Button _sparBtn = null!;
    private Button _pauseBtn = null!;
    private Label _statusLabel = null!;

    private int _selectedTravelNode = -1;
    private long _selectedSparTarget = -1;

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        _worldView = GetNode<WorldView>("../WorldView");

        // 面板底色
        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.07f, 0.09f, 0.92f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // 状态标签
        _statusLabel = new Label();
        _statusLabel.Text = "选择行动...";
        _statusLabel.Position = new Vector2(8, 4);
        _statusLabel.Size = new Vector2(180, 18);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.65f, 0.5f));
        _statusLabel.AddThemeFontSizeOverride("font_size", 11);
        AddChild(_statusLabel);

        // 按钮布局
        int btnY = 24;
        int btnW = 56;
        int btnH = 28;
        int gap = 4;

        _trainBtn = MakeBtn("修炼", 8, btnY, btnW, btnH, new Color(0.25f, 0.35f, 0.22f));
        _trainBtn.Pressed += () => DoAction("train");

        _travelBtn = MakeBtn("游历", 8 + btnW + gap, btnY, btnW, btnH, new Color(0.22f, 0.28f, 0.35f));
        _travelBtn.Pressed += () => DoAction("travel");
        _travelBtn.Disabled = true;

        _sparBtn = MakeBtn("切磋", 8 + (btnW + gap) * 2, btnY, btnW, btnH, new Color(0.35f, 0.22f, 0.18f));
        _sparBtn.Pressed += () => DoAction("spar");
        _sparBtn.Disabled = true;

        _pauseBtn = MakeBtn("暂停", 8 + (btnW + gap) * 3, btnY, btnW, btnH, new Color(0.3f, 0.25f, 0.15f));
        _pauseBtn.Pressed += TogglePause;

        // 监听 WorldView 选择
        _worldView.OnTargetSelected += OnTargetSelected;

        SetSize(new Vector2(260, 58));
        Position = new Vector2(270, 400);
    }

    private Button MakeBtn(string text, float x, float y, float w, float h, Color color)
    {
        var btn = new Button();
        btn.Text = text;
        btn.Position = new Vector2(x, y);
        btn.Size = new Vector2(w, h);
        btn.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        btn.AddThemeFontSizeOverride("font_size", 11);

        var normal = new StyleBoxFlat();
        normal.BgColor = color;
        normal.BorderWidthLeft = normal.BorderWidthRight = 1;
        normal.BorderWidthTop = normal.BorderWidthBottom = 1;
        normal.BorderColor = new Color(0.4f, 0.35f, 0.2f, 0.6f);
        normal.CornerRadiusTopLeft = normal.CornerRadiusTopRight = 3;
        normal.CornerRadiusBottomLeft = normal.CornerRadiusBottomRight = 3;
        btn.AddThemeStyleboxOverride("normal", normal);

        AddChild(btn);
        return btn;
    }

    private void OnTargetSelected(string type, int data)
    {
        if (type == "node")
        {
            _selectedTravelNode = data;
            _selectedSparTarget = -1;
            _travelBtn.Disabled = false;
            _sparBtn.Disabled = true;
            _statusLabel.Text = $"目标: 节点 {data} → 可游历";
        }
        else if (type == "char")
        {
            _selectedSparTarget = data;
            _selectedTravelNode = -1;
            _sparBtn.Disabled = false;
            _travelBtn.Disabled = true;
            _statusLabel.Text = $"目标: 角色 #{data} → 可切磋";
        }
    }

    private void DoAction(string action)
    {
        var world = _bridge.World;
        var player = _bridge.PlayerCharacter;
        if (world == null || player == null) return;

        switch (action)
        {
            case "train":
                // 自动修炼最弱战斗属性
                _bridge.QueuePlayerAction(new Jianghu.Actions.TrainChoice(
                    GetWeakestStat(player)));
                _statusLabel.Text = "修炼中...";
                break;
            case "travel":
                if (_selectedTravelNode >= 0)
                {
                    _bridge.QueuePlayerAction(new Jianghu.Actions.TravelChoice(
                        new Jianghu.Model.NodeId(_selectedTravelNode)));
                    _statusLabel.Text = $"游历至节点 {_selectedTravelNode}";
                }
                break;
            case "spar":
                if (_selectedSparTarget >= 0)
                {
                    _bridge.QueuePlayerAction(new Jianghu.Actions.SparChoice(
                        new Jianghu.Model.CharacterId(_selectedSparTarget)));
                    _statusLabel.Text = $"切磋角色 #{_selectedSparTarget}";
                }
                break;
        }

        _travelBtn.Disabled = true;
        _sparBtn.Disabled = true;
        _selectedTravelNode = -1;
        _selectedSparTarget = -1;
    }

    private static Jianghu.Stats.StatKind GetWeakestStat(Jianghu.Model.Character c)
    {
        var stats = new[] { Jianghu.Stats.StatKind.Force, Jianghu.Stats.StatKind.Internal,
            Jianghu.Stats.StatKind.Constitution, Jianghu.Stats.StatKind.Insight };
        Jianghu.Stats.StatKind weakest = stats[0];
        int min = int.MaxValue;
        foreach (var k in stats)
            if (c.Stats.Get(k) < min) { min = c.Stats.Get(k); weakest = k; }
        return weakest;
    }

    private void TogglePause()
    {
        _bridge.Paused = !_bridge.Paused;
        _pauseBtn.Text = _bridge.Paused ? "继续" : "暂停";
        _statusLabel.Text = _bridge.Paused ? "已暂停——选择行动" : "进行中...";
    }
}
