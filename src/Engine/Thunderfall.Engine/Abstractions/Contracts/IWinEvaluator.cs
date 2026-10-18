using System.Collections.Immutable;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Abstractions.Contracts;

public interface IWinEvaluator
{
    ImmutableArray<WinningGroup> Describe(ReadOnlySpan<Cell> board, CompiledDefinition definition);
    WinEvaluation Evaluate(
        ReadOnlySpan<Cell> board,
        CompiledDefinition definition,
        Span<int> counts
    );
}
