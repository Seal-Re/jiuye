using System;
using System.Linq;
using Xunit;

public class CorePurityTests
{
    [Fact]
    public void Core_does_not_reference_forbidden_assemblies()
    {
        var refs = typeof(Jianghu.Sim.World).Assembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("System.Net.Http", refs);
        Assert.DoesNotContain("System.Console", refs);
    }

    [Fact]
    public void Core_assembly_loads_from_test_runtime()
    {
        Assert.NotNull(typeof(Jianghu.Sim.World).Assembly.Location);
    }
}
