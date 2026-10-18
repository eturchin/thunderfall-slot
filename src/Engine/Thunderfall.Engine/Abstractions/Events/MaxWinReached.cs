namespace Thunderfall.Engine.Abstractions.Events;

public sealed record MaxWinReached(
    int SequenceNumber,
    long NominalAward,
    long Award,
    long CycleAward,
    int AbandonedSpins
) : MathEvent(SequenceNumber);
