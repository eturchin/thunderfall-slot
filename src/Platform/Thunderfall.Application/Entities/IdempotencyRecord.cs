namespace Thunderfall.Application.Entities;

public sealed record IdempotencyRecord(
    Guid SessionId,
    Guid Key,
    string Fingerprint,
    Guid RoundId,
    DateTimeOffset CreatedAt
);
