using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Abstractions.Events;

public sealed record CascadeCompleted(int SequenceNumber, int StepIndex, Board Board)
    : MathEvent(SequenceNumber);
