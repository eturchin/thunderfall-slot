using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Abstractions.Contracts;

public interface ICascadePolicy
{
    void Apply(
        Span<Cell> board,
        ulong removed,
        CompiledDefinition definition,
        CompiledProfile profile,
        IRandomSource rng,
        ref int identity,
        List<SymbolDrop>? drops = null,
        List<SymbolSpawn>? spawns = null
    );
}
