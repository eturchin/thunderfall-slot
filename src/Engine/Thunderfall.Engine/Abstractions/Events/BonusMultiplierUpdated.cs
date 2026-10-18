namespace Thunderfall.Engine.Abstractions.Events;

public sealed record BonusMultiplierUpdated(
    int SequenceNumber,
    long Before,
    long Collected,
    long After
) : MathEvent(SequenceNumber);
