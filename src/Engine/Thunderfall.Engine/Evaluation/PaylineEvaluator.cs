using System.Collections.Immutable;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Evaluation;

public sealed class PaylineEvaluator
{
    public (long Ticks, ImmutableArray<WinningGroup> Groups) Evaluate(
        ReadOnlySpan<Cell> board,
        CompiledDefinition d,
        bool trace
    )
    {
        long total = 0;
        var groups = trace ? ImmutableArray.CreateBuilder<WinningGroup>() : null;
        var g = d.Definition;
        for (var line = 0; line < g.Paylines.Length; line++)
        {
            long best = 0;
            SymbolId symbol = default;
            var matches = 0;
            for (var s = 1; s <= 8; s++)
            {
                var n = 0;
                while (n < g.Layout.Columns)
                {
                    var id = board[g.Layout.Index(n, g.Paylines[line][n])].Symbol;
                    if (id != (SymbolId)s && id != SymbolId.Wild)
                    {
                        break;
                    }

                    n++;
                }
                var ticks = d.Payout((SymbolId)s, n);
                if (ticks > best)
                {
                    best = ticks;
                    symbol = (SymbolId)s;
                    matches = n;
                }
            }
            total = checked(total + best);
            if (trace && best > 0)
            {
                groups!.Add(
                    new(
                        symbol,
                        matches,
                        best,
                        Enumerable
                            .Range(0, matches)
                            .Select(c => g.Layout.Index(c, g.Paylines[line][c]))
                            .ToImmutableArray(),
                        line + 1
                    )
                );
            }
        }
        return (total, groups?.ToImmutable() ?? []);
    }
}
