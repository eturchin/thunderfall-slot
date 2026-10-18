namespace Thunderfall.Engine.Simulation.Simulation;

public sealed record SimulationOptions(
    long PaidCycles = 1_000_000,
    ulong Seed = 123456,
    ulong StreamId = 0,
    int Workers = 1,
    long? Stake = null
);
