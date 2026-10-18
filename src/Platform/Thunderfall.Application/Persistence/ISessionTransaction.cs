using Thunderfall.Application.Entities;
using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Application.Persistence;

public interface ISessionTransaction
{
    GameSession Session
    {
        get;
    }
    long Balance
    {
        get; set;
    }
    GameMathState State
    {
        get; set;
    }
    Task<(IdempotencyRecord Idempotency, RoundRecord Round)?> FindOperationAsync(
        Guid key,
        CancellationToken cancellationToken
    );
    void Add(WalletEntry entry);
    void Add(RoundRecord round);
    void Add(IdempotencyRecord operation);
}
