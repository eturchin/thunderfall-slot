using Microsoft.EntityFrameworkCore;
using Thunderfall.Application;
using Thunderfall.Application.Entities;
using Thunderfall.Application.Persistence;
using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Infrastructure;

internal sealed class PostgresSessionTransaction(
    GameDbContext db,
    GameSession session,
    DemoWallet wallet,
    GameMathState state
) : ISessionTransaction
{
    public GameSession Session => session;

    public long Balance
    {
        get => wallet.Balance;
        set => wallet.Balance = value;
    }

    public GameMathState State { get; set; } = state;

    public async Task<(IdempotencyRecord Idempotency, RoundRecord Round)?> FindOperationAsync(
        Guid key,
        CancellationToken cancellationToken
    )
    {
        var operation = await db
            .IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(
                record => record.SessionId == session.Id && record.Key == key,
                cancellationToken
            );
        if (operation is null)
        {
            return null;
        }

        var round = await db
            .RoundRecords.AsNoTracking()
            .SingleAsync(
                record => record.SessionId == session.Id && record.Id == operation.RoundId,
                cancellationToken
            );
        return (operation, round);
    }

    public void Add(WalletEntry entry) => db.WalletLedger.Add(entry);

    public void Add(RoundRecord round) => db.RoundRecords.Add(round);

    public void Add(IdempotencyRecord operation) => db.IdempotencyRecords.Add(operation);
}
