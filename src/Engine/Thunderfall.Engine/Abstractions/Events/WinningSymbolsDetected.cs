using System.Collections.Immutable;
using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Engine.Abstractions.Events;

public sealed record WinningSymbolsDetected(int SequenceNumber, ImmutableArray<WinningGroup> Groups)
    : MathEvent(SequenceNumber);
