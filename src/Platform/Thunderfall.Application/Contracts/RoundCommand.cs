namespace Thunderfall.Application.Contracts;

public sealed record RoundCommand(long? Stake, Guid IdempotencyKey, bool BuyBonus = false);
