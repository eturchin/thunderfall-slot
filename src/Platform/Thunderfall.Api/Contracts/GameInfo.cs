using Thunderfall.Engine.Primitives;

namespace Thunderfall.Api.Contracts;

public sealed record GameInfo(string GameId, string MathVersion, string RulesetHash, string Name, string Kind, Layout Layout, System.Collections.Immutable.ImmutableArray<long> Stakes, System.Collections.Immutable.ImmutableArray<SymbolId> Symbols);
