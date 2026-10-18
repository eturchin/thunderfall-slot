using System.Collections.Immutable;

namespace Thunderfall.Engine.Abstractions.Definitions;

public sealed record GenerationProfile(
    ImmutableArray<SymbolWeight> InitialWeights,
    ImmutableArray<SymbolWeight> RefillWeights,
    ImmutableArray<ValueWeight> OrbValues
);
