using System.Collections.Generic;

namespace Jianghu.Model
{
    public sealed class Sect
    {
        public int Id { get; }
        public string Name { get; }
        public List<CharacterId> Disciples { get; } = new List<CharacterId>();
        public Sect(int id, string name) { Id = id; Name = name; }
    }
}
