using System.Collections.Immutable;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Evaluation;

public static class ReferenceEvaluator
{
    // Deliberately independent LINQ implementation used for differential tests/replay, never the simulation hot loop.
    public static ImmutableArray<WinningGroup> Evaluate(Board board, GameDefinition definition)
    {
        var result = ImmutableArray.CreateBuilder<WinningGroup>();
        foreach (var entry in definition.Paytable.OrderBy(p => p.Key))
        {
            var positions = board
                .Cells.Select((c, i) => (c, i))
                .Where(x => x.c.Symbol == entry.Key)
                .Select(x => x.i)
                .ToImmutableArray();
            var pay = entry
                .Value.Where(b => positions.Length >= b.MinimumCount)
                .Select(b => b.PayTicks)
                .DefaultIfEmpty(0)
                .Last();
            if (pay > 0)
            {
                result.Add(new(entry.Key, positions.Length, pay, positions));
            }
        }
        return result.ToImmutable();
    }
}
