using System.Collections.Immutable;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Evaluation;

public sealed class CountAnywhereEvaluator : IWinEvaluator
{
    public ImmutableArray<WinningGroup> Describe(
        ReadOnlySpan<Cell> board,
        CompiledDefinition definition
    ) => Groups(board, definition);

    public WinEvaluation Evaluate(ReadOnlySpan<Cell> board, CompiledDefinition d, Span<int> counts)
    {
        counts.Clear();
        foreach (var cell in board)
        {
            if (cell.Symbol >= SymbolId.Crown && cell.Symbol <= SymbolId.YellowGem)
            {
                counts[(int)cell.Symbol]++;
            }
        }

        long ticks = 0;
        uint paying = 0;
        for (var s = 1; s <= 8; s++)
        {
            var award = d.Payout((SymbolId)s, counts[s]);
            if (award > 0)
            {
                ticks = checked(ticks + award);
                paying |= 1u << s;
            }
        }
        ulong removed = 0;
        for (var i = 0; i < board.Length; i++)
        {
            if ((paying & (1u << (int)board[i].Symbol)) != 0)
            {
                removed |= 1UL << i;
            }
        }

        return new(removed, ticks);
    }

    public static ImmutableArray<WinningGroup> Groups(
        ReadOnlySpan<Cell> board,
        CompiledDefinition d
    )
    {
        var cells = board.ToArray();
        return cells
            .Where(c => c.Symbol >= SymbolId.Crown && c.Symbol <= SymbolId.YellowGem)
            .GroupBy(c => c.Symbol)
            .OrderBy(g => g.Key)
            .Where(g => d.Payout(g.Key, g.Count()) > 0)
            .Select(g => new WinningGroup(
                g.Key,
                g.Count(),
                d.Payout(g.Key, g.Count()),
                cells
                    .Select((c, i) => (c, i))
                    .Where(x => x.c.Symbol == g.Key)
                    .Select(x => x.i)
                    .ToImmutableArray()
            ))
            .ToImmutableArray();
    }
}
