using System.Collections.Immutable;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Abstractions.Contracts;

public sealed record WinningGroup(
    SymbolId Symbol,
    int Count,
    long PayTicks,
    ImmutableArray<int> Positions,
    int? LineId = null
);
