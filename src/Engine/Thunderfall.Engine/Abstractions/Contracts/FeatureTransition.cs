namespace Thunderfall.Engine.Abstractions.Contracts;

public readonly record struct FeatureTransition(
    GameMathState State,
    long Award,
    long NominalAward,
    long EffectiveMultiplier,
    bool CapReached,
    int AbandonedSpins,
    bool Triggered,
    bool Retriggered,
    bool Completed
);
