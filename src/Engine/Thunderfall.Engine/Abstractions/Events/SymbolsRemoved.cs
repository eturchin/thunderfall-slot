using System.Collections.Immutable;

namespace Thunderfall.Engine.Abstractions.Events;

public sealed record SymbolsRemoved(int SequenceNumber, ImmutableArray<int> Positions)
    : MathEvent(SequenceNumber);
