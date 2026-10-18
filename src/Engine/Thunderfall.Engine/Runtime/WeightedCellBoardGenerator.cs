using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Runtime;

public sealed class WeightedCellBoardGenerator : IBoardGenerator
{
    public void Generate(
        Span<Cell> board,
        CompiledDefinition definition,
        CompiledProfile profile,
        IRandomSource rng,
        ref int identity
    )
    {
        for (var i = 0; i < board.Length; i++)
        {
            board[i] = WeightedRefillGenerator.Draw(profile.Initial, profile, rng, ref identity);
        }
    }
}
