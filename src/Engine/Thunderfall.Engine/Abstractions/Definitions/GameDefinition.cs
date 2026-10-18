using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Abstractions.Definitions;

public sealed record GameDefinition(
    string GameId,
    string MathVersion,
    string Name,
    string Kind,
    Layout Layout,
    ImmutableArray<SymbolId> Symbols,
    ImmutableDictionary<SymbolId, ImmutableArray<PayBand>> Paytable,
    GenerationProfile BaseGenerationProfile,
    GenerationProfile BonusGenerationProfile,
    FeatureRules FeatureRules,
    ImmutableArray<long> Stakes,
    long MaxWinMultiplier,
    ResourceLimits ResourceLimits,
    ImmutableArray<ImmutableArray<SymbolId>> ReelStrips,
    ImmutableArray<ImmutableArray<int>> Paylines
)
{
    [JsonIgnore]
    public string RulesetHash => DefinitionJson.Hash(this);
}
