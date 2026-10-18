using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Application.Contracts;

public sealed record SessionView(
    Guid Id,
    string GameId,
    string MathVersion,
    string RulesetHash,
    long Balance,
    GameMathState FeatureState,
    long Version
);
