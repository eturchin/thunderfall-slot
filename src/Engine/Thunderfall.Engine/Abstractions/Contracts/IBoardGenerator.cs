using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Abstractions.Contracts;

public interface IBoardGenerator
{
    void Generate(
        Span<Cell> board,
        CompiledDefinition definition,
        CompiledProfile profile,
        IRandomSource rng,
        ref int identity
    );
}
