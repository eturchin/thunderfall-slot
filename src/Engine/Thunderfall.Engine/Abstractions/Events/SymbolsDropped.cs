using System.Collections.Immutable;
using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Engine.Abstractions.Events;

public sealed record SymbolsDropped(int SequenceNumber, ImmutableArray<SymbolDrop> Moves)
    : MathEvent(SequenceNumber);
