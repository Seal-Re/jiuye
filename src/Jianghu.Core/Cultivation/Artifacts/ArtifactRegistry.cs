using System;
using System.Collections.Generic;
using System.Linq;

namespace Jianghu.Cultivation.Artifacts
{
    public sealed class ArtifactRegistry
    {
        readonly Dictionary<string, ArtifactDef> _all;
        readonly ILookup<ArtifactGrade, ArtifactDef> _byGrade;
        readonly ILookup<ArtifactForm, ArtifactDef> _byForm;
        readonly List<ArtifactDef> _uniques;

        public ArtifactRegistry(IReadOnlyList<ArtifactDef> artifacts)
        {
            _all = new Dictionary<string, ArtifactDef>();
            foreach (var a in artifacts)
            {
                if (_all.ContainsKey(a.Id))
                    throw new InvalidOperationException($"Artifact duplicate id: {a.Id}");
                _all[a.Id] = a;
            }
            _byGrade = artifacts.ToLookup(a => a.Grade);
            _byForm = artifacts.ToLookup(a => a.Form);
            _uniques = artifacts.Where(a => a.Rarity == EffectRarity.Unique).ToList();
        }

        public ArtifactDef Get(string id)
        {
            if (!_all.TryGetValue(id, out var a))
                throw new KeyNotFoundException($"Artifact not found: {id}");
            return a;
        }

        public IReadOnlyList<ArtifactDef> All => _all.Values.ToList();
        public IReadOnlyList<ArtifactDef> ByGrade(ArtifactGrade g) => _byGrade[g].ToList();
        public IReadOnlyList<ArtifactDef> ByForm(ArtifactForm f) => _byForm[f].ToList();
        public IReadOnlyList<ArtifactDef> ByFunction(ArtifactFunction f)
            => _all.Values.Where(a => a.PrimaryFunc == f || a.SecondaryFunc == f).ToList();
        public IReadOnlyList<ArtifactDef> UniqueArtifacts => _uniques;
    }
}
