namespace Thunderfall.Engine.Abstractions.Events;

public sealed record RoundWinCalculated(
    int SequenceNumber,
    long PayTicks,
    decimal RawWin,
    long EffectiveMultiplier,
    long NominalAward,
    long Award
) : MathEvent(SequenceNumber);
