using System.Threading;
using System.Threading.Tasks;
using Jianghu.Actions;

namespace Jianghu.Decide
{
    public interface IBrain
    {
        ValueTask<ActionChoice> DecideAsync(DecisionContext ctx, CancellationToken ct);
    }
}
