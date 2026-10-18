using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Api.Contracts;

public sealed record GameRules(string GameId, string MathVersion, string RulesetHash, string Kind, Layout Layout, System.Collections.Immutable.ImmutableDictionary<SymbolId, System.Collections.Immutable.ImmutableArray<PayBand>> Paytable, FeatureRules FeatureRules, long MaxWinMultiplier, int CreditScale, long PayScale, BonusBuyOptions? BonusBuy = null);
