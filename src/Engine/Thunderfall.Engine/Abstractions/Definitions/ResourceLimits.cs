namespace Thunderfall.Engine.Abstractions.Definitions;

public sealed record ResourceLimits(int MaxCascades = 2048, int MaxBonusRounds = 100000);
