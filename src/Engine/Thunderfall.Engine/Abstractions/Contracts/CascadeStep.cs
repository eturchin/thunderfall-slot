using System.Collections.Immutable;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Abstractions.Contracts;

public sealed record CascadeStep(
    int StepIndex,
    Board BoardBefore,
    ImmutableArray<WinningGroup> WinningGroups,
    ImmutableArray<int> RemovedPositions,
    ImmutableArray<SymbolDrop> GravityMoves,
    ImmutableArray<SymbolSpawn> NewSymbols,
    Board BoardAfter,
    decimal BaseWin,
    ImmutableArray<OrbCollection> MultiplierEvents
);
