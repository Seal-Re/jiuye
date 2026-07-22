using Godot;
using Jianghu.Model;

namespace GodotHost;

/// <summary>
/// 角色列表侧栏——按节点分组显示全部在世角色。
/// 每行：Sprite占位 + 名字 + 路径缩写 + 战力。
/// 点击行→CharInfoCard。
/// </summary>
public partial class CharacterListPanel : Control
{
    private WorldBridge _bridge = null!;
    private CharInfoCard? _infoCard;
    private VBoxContainer _list = null!;
    private ScrollContainer _scroll = null!;
    private RichTextLabel _header = null!;

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        _infoCard = GetNode<CharInfoCard>("../CharInfoCard");

        // 面板底色
        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.07f, 0.09f, 0.88f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // 水墨边框
        AddBorderLine(0, 0, 200, 260);

        // 标题
        _header = new RichTextLabel();
        _header.BbcodeEnabled = true;
        _header.FitContent = true;
        _header.Text = "[center][color=#c8a860]　江湖人物录　[/color][/center]";
        _header.Position = new Vector2(0, 4);
        _header.Size = new Vector2(200, 20);
        AddChild(_header);

        // 滚动列表
        _scroll = new ScrollContainer();
        _scroll.Position = new Vector2(4, 26);
        _scroll.Size = new Vector2(192, 228);

        _list = new VBoxContainer();
        _list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.AddChild(_list);
        AddChild(_scroll);

        SetSize(new Vector2(200, 260));
        Position = new Vector2(500, 0);  // 右边侧栏

        // 初始化后刷新
        CallDeferred(nameof(Refresh));
    }

    public void Refresh()
    {
        // 清空旧行
        foreach (var child in _list.GetChildren())
            child.QueueFree();

        var world = _bridge.World;
        if (world == null) return;

        // 按节点分组
        var byNode = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Character>>();
        foreach (var c in world.AliveCharacters())
        {
            int n = c.Node.Value;
            if (!byNode.ContainsKey(n)) byNode[n] = new System.Collections.Generic.List<Character>();
            byNode[n].Add(c);
        }

        foreach (var kv in byNode)
        {
            // 节点标题
            var nodeLabel = new RichTextLabel();
            nodeLabel.BbcodeEnabled = true;
            nodeLabel.FitContent = true;
            nodeLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            nodeLabel.Text = $"[color=#706850]—— 节点 {kv.Key} ——[/color]";
            _list.AddChild(nodeLabel);

            // 角色行
            foreach (var c in kv.Value)
            {
                var row = new Button();
                row.Flat = true;
                row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.CustomMinimumSize = new Vector2(0, 20);
                row.AddThemeColorOverride("font_color", new Color(0.85f, 0.80f, 0.65f));
                row.AddThemeFontSizeOverride("font_size", 11);
                row.Alignment = HorizontalAlignment.Left;

                string pathShort = c.Cultivation?.PathId?[..System.Math.Min(4, c.Cultivation.PathId.Length)] ?? "--";
                int pe = 0;
                if (c.Cultivation != null)
                {
                    var reg = new Jianghu.Cultivation.PathRegistry(new Jianghu.Cultivation.CodePathSource());
                    var def = reg.ById(c.Cultivation.PathId);
                    pe = Jianghu.Cultivation.PowerEngine.Evaluate(c.Cultivation, c.Stats, def, world.Limits);
                }
                row.Text = $"  {c.Persona.Name[..System.Math.Min(2, c.Persona.Name.Length)]}  {pathShort}  {pe}";

                var cCapture = c;
                row.Pressed += () => _infoCard?.ShowCharacter(cCapture, _bridge);
                _list.AddChild(row);
            }
        }
    }

    private static void AddBorderLine(float x, float y, float w, float h)
    {
        // 调用者在 AddChild 前使用，这里直接返回边框 rects 供父节点添加
    }

    // 每步刷新
    public void OnStep() => CallDeferred(nameof(Refresh));
}
