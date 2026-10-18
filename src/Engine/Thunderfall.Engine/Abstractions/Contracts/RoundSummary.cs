namespace Thunderfall.Engine.Abstractions.Contracts;

public readonly record struct RoundSummary(
    long Award,
    long PayTicks,
    long CollectedMultiplier,
    int Cascades,
    int Scatters,
    GameMathState State,
    bool CapReached,
    bool Triggered,
    bool Retriggered,
    long EffectiveMultiplier,
    OrbHistogram Orbs = default
);
