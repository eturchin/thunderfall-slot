namespace Thunderfall.Engine.Simulation.Simulation;

public sealed record VolatilityMetrics(
    double StandardDeviation,
    double Variance,
    double ZeroAwardFrequency,
    double HundredXFrequency,
    double CapFrequency
);
