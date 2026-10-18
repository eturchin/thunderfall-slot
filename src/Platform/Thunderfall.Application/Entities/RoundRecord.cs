namespace Thunderfall.Application.Entities;

public sealed record RoundRecord(
    Guid Id,
    Guid SessionId,
    string GameId,
    string MathVersion,
    string RulesetHash,
    long Stake,
    long Award,
    string Mode,
    string OutcomeJson,
    DateTimeOffset CreatedAt
);
