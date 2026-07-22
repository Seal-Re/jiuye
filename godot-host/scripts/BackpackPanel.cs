using System.Collections.Generic;
using Godot;

namespace GodotHost;

/// <summary>
/// 背包面板——丹药/法宝/功法书物品格。
/// 6格背包，可查看+使用（当前为视觉原型，物品数据来自CultivationState.Resources）。
/// </summary>
public partial class BackpackPanel : Control
{
    private WorldBridge _bridge = null!;
    private GridContainer _grid = null!;
    private RichTextLabel _itemInfo = null!;

    // 背包物品（简化：从 Resources 推导）
    private readonly List<(string Name, string Desc, Color Color)> _items = new();

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.07f, 0.09f, 0.9f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var title = new RichTextLabel();
        title.BbcodeEnabled = true;
        title.FitContent = true;
        title.Text = "[center][color=#c8a860]　行囊　[/color][/center]";
        title.Position = new Vector2(0, 4);
        title.Size = new Vector2(200, 20);
        AddChild(title);

        // 6格网格
        _grid = new GridContainer();
        _grid.Columns = 3;
        _grid.Position = new Vector2(10, 28);
        _grid.Size = new Vector2(180, 120);
        AddChild(_grid);

        _itemInfo = new RichTextLabel();
        _itemInfo.BbcodeEnabled = true;
        _itemInfo.FitContent = true;
        _itemInfo.Position = new Vector2(10, 152);
        _itemInfo.Size = new Vector2(180, 44);
        AddChild(_itemInfo);

        SetSize(new Vector2(200, 200));
        Position = new Vector2(540, 400);

        CallDeferred(nameof(Refresh));
    }

    public void Refresh()
    {
        foreach (var c in _grid.GetChildren()) c.QueueFree();
        _items.Clear();

        var st = _bridge.PlayerCharacter?.Cultivation;
        if (st == null) return;

        // 从 Resources 派生背包物品
        if (st.Resources.TryGetValue("pillStock", out int pills) && pills > 0)
            _items.Add(("丹药", $"库存 {pills} 颗", new Color(0.8f, 0.5f, 0.3f)));
        if (st.Resources.TryGetValue("flameTier", out int flame) && flame > 0)
            _items.Add(("异火", $"阶位 {flame}", new Color(1f, 0.6f, 0.2f)));
        if (st.Resources.TryGetValue("recipeCount", out int recipes) && recipes > 0)
            _items.Add(("丹方", $"{recipes} 张", new Color(0.5f, 0.7f, 0.5f)));

        // 空物品（补满6格）
        while (_items.Count < 6)
            _items.Add(("空", "", new Color(0.3f, 0.3f, 0.3f)));

        for (int i = 0; i < _items.Count; i++)
        {
            var slot = MakeSlot(_items[i], i);
            _grid.AddChild(slot);
        }
    }

    private Button MakeSlot((string Name, string Desc, Color Color) item, int index)
    {
        var btn = new Button();
        btn.Text = item.Name;
        btn.CustomMinimumSize = new Vector2(54, 36);
        btn.AddThemeColorOverride("font_color", item.Color);
        btn.AddThemeFontSizeOverride("font_size", 10);

        var normal = new StyleBoxFlat();
        normal.BgColor = new Color(0.12f, 0.11f, 0.10f, 1f);
        normal.BorderWidthLeft = normal.BorderWidthRight = 1;
        normal.BorderWidthTop = normal.BorderWidthBottom = 1;
        normal.BorderColor = new Color(0.3f, 0.25f, 0.15f, 0.5f);
        btn.AddThemeStyleboxOverride("normal", normal);

        btn.Pressed += () => _itemInfo.Text =
            $"[color=#b0a080]{item.Name}[/color]\n[color=#888]{item.Desc}[/color]";
        return btn;
    }
}
