namespace Thunderfall.Application.Entities;

public sealed record WalletEntry(
    Guid Id,
    Guid SessionId,
    Guid? RoundId,
    string Type,
    long Delta,
    long ClosingBalance,
    DateTimeOffset CreatedAt
);
