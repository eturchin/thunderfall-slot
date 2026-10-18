namespace Thunderfall.Application.Contracts;

public sealed record RoundHistoryItem(
    Guid RoundId,
    string GameId,
    string MathVersion,
    string Mode,
    long Stake,
    long Award,
    DateTimeOffset CreatedAt
);
