using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Runtime;

public static class WeightedRefillGenerator
{
    public static Cell Draw(
        WeightedSampler sampler,
        CompiledProfile profile,
        IRandomSource rng,
        ref int identity
    )
    {
        var id = (SymbolId)sampler.Sample(rng);
        return new(
            id,
            id == SymbolId.StormOrb ? profile.OrbValues.Sample(rng) : 0,
            checked(++identity)
        );
    }
}
