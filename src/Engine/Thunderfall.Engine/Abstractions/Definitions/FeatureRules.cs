namespace Thunderfall.Engine.Abstractions.Definitions;

public sealed record FeatureRules(
    int TriggerScatters = 4,
    int InitialSpins = 15,
    int RetriggerScatters = 3,
    int RetriggerSpins = 5
);
