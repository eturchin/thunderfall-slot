namespace Thunderfall.Engine.Abstractions.Contracts;

public readonly record struct ExecutionOptions(
    bool Trace = true,
    CancellationToken CancellationToken = default
);
