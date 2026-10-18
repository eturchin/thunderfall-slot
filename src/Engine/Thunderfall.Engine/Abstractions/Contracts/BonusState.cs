namespace Thunderfall.Engine.Abstractions.Contracts;

public sealed record BonusState(
    Guid BonusSessionId,
    string GameId,
    string MathVersion,
    long OriginalStake,
    int RemainingSpins,
    long PlayedSpins,
    long PersistentMultiplier,
    long AccumulatedWin,
    long RetriggerCount,
    int StateVersion,
    long CycleAward
)
{
    public bool Active => RemainingSpins > 0;
}
