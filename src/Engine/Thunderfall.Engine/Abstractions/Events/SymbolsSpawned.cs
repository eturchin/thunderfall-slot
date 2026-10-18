using System.Collections.Immutable;
using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Engine.Abstractions.Events;

public sealed record SymbolsSpawned(int SequenceNumber, ImmutableArray<SymbolSpawn> Symbols)
    : MathEvent(SequenceNumber);
