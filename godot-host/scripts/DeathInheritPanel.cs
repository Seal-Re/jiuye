using Godot;
using Jianghu.Cultivation;
using Jianghu.Model;

namespace GodotHost;

/// <summary>
/// 死亡继承面板——Play P3。
/// 玩家角色死亡时弹出：显示死亡总结 + 选择继承人（在世角色中选）。
/// 继承人获得部分属性/路径加成。
/// </summary>
public partial class DeathInheritPanel : Control
{
    private WorldBridge _bridge = null!;
    private CharCreatePanel? _createPanel;
    private RichTextLabel _deathLabel = null!;
    private Button _inheritBtn = null!;
    private Button _newCharBtn = null!;

    private long _deadPlayerId = -1;

    public override void _Ready()
    {
        _bridge = GetNode<WorldBridge>("../WorldBridge");
        _createPanel = GetNode<CharCreatePanel>("../CharCreatePanel");
        _bridge.OnDomainEvent += OnEvent;

        // 面板
        var bg = new ColorRect();
        bg.Color = new Color(0.06f, 0.04f, 0.03f, 0.95f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        _deathLabel = new RichTextLabel();
        _deathLabel.BbcodeEnabled = true;
        _deathLabel.FitContent = true;
        _deathLabel.Position = new Vector2(20, 30);
        _deathLabel.Size = new Vector2(260, 80);
        AddChild(_deathLabel);

        _inheritBtn = new Button();
        _inheritBtn.Text = "继承衣钵（随机在世角色）";
        _inheritBtn.Position = new Vector2(30, 120);
        _inheritBtn.Size = new Vector2(240, 32);
        _inheritBtn.Pressed += OnInherit;
        AddChild(_inheritBtn);

        _newCharBtn = new Button();
        _newCharBtn.Text = "新踏入江湖";
        _newCharBtn.Position = new Vector2(30, 160);
        _newCharBtn.Size = new Vector2(240, 32);
        _newCharBtn.Pressed += OnNewChar;
        AddChild(_newCharBtn);

        SetSize(new Vector2(300, 210));
        Position = new Vector2(200, 150);
        Hide();
    }

    private void OnEvent(string line)
    {
        var player = _bridge.PlayerCharacter;
        if (player == null) return;

        // 检测玩家死亡
        if (line.Contains(player.Persona.Name) && line.Contains("寿尽"))
        {
            ShowDeath(player);
        }
    }

    private void ShowDeath(Character player)
    {
        _deadPlayerId = player.Id.Value;
        long age = player.Age;
        string pathName = player.Cultivation != null
            ? new PathRegistry(new CodePathSource()).ById(player.Cultivation.PathId).Name
            : "未入道";

        _deathLabel.Text =
            $"[center][color=#c8a860]　{player.Persona.Name} 仙逝　[/color][/center]\n" +
            $"[color=#a09080]享年 {age} 岁 | {pathName}[/color]\n" +
            $"[color=#888]江湖路远，后继有人...[/color]";

        Show();
        _bridge.Paused = true;
    }

    private void OnInherit()
    {
        var world = _bridge.World;
        if (world == null) return;

        // 随机选一个在世 NPC 作为继承人
        var alive = world.AliveCharacters();
        if (alive.Count == 0) { OnNewChar(); return; }

        var heir = alive[new System.Random().Next(alive.Count)];
        var def = heir.Cultivation != null
            ? new PathRegistry(new CodePathSource()).ById(heir.Cultivation.PathId) : null;

        // 继承：改名 + 加前缀
        string heirName = $"小{heir.Persona.Name}";
        heir.Persona = new Persona(heirName, "继承者", heir.Persona.Origin, heir.Persona.Archetype, null);

        _bridge.PlayerCharacter = heir;
        _bridge.Paused = false;
        GD.Print($"[DeathInherit] {heirName} 继承衣钵——{def?.Name ?? "未知路"}");
        Hide();
    }

    private void OnNewChar()
    {
        _bridge.PlayerCharacter = null;
        _bridge.Paused = false;
        Hide();
        _createPanel?.Show();
    }
}
