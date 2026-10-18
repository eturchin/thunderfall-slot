using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Abstractions.Events;

public sealed record BoardGenerated(int SequenceNumber, Board Board) : MathEvent(SequenceNumber);
