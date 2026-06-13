using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

public class ChronicleTests
{
    [Fact]
    public void Projects_events_to_readable_text_deterministically()
    {
        var ch = new Chronicle();
        ch.Append(new CharacterTrained(10, new CharacterId(1), StatKind.Force, 2), n => "甲");
        ch.Append(new DuelResolved(11, new CharacterId(1), new CharacterId(2), 5), n => n.Value == 1 ? "甲" : "乙");
        Assert.Equal(2, ch.Lines.Count);
        Assert.Contains("甲", ch.Lines[0]);
        Assert.Contains("切磋", ch.Lines[1]);
    }
}
