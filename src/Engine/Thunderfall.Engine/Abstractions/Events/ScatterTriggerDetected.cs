namespace Thunderfall.Engine.Abstractions.Events;

public sealed record ScatterTriggerDetected(int SequenceNumber, int ScatterCount, bool IsRetrigger)
    : MathEvent(SequenceNumber);
