using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Thunderfall.Application;
using Thunderfall.Application.Contracts;
using Thunderfall.Application.Entities;
using Thunderfall.Application.Errors;
using Thunderfall.Application.Persistence;
using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;

namespace Thunderfall.Infrastructure;

public sealed class PostgresSessionStore(IDbContextFactory<GameDbContext> contextFactory)
    : ISessionStore
{
    public async Task CreateAsync(
        GameSession session,
        long balance,
        CancellationToken cancellationToken
    )
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.GameSessions.Add(session);
        db.DemoWallets.Add(new() { SessionId = session.Id, Balance = balance });
        db.FeatureSessions.Add(
            new()
            {
                SessionId = session.Id,
                MathVersion = session.MathVersion,
                StateJson = JsonSerializer.Serialize(GameMathState.Empty, DefinitionJson.Options),
                UpdatedAt = session.CreatedAt,
            }
        );
        db.WalletLedger.Add(
            new(
                Guid.NewGuid(),
                session.Id,
                null,
                "InitialCredit",
                balance,
                balance,
                session.CreatedAt
            )
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SessionSnapshot?> ReadAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var snapshot = await (
            from s in db.GameSessions.AsNoTracking()
            join w in db.DemoWallets.AsNoTracking() on s.Id equals w.SessionId
            join f in db.FeatureSessions.AsNoTracking() on s.Id equals f.SessionId
            where s.Id == id
            select new
            {
                s,
                w.Balance,
                f.StateJson,
            }
        ).SingleOrDefaultAsync(cancellationToken);
        return snapshot is null
            ? null
            : new(
                snapshot.s,
                snapshot.Balance,
                JsonSerializer.Deserialize<GameMathState>(
                    snapshot.StateJson,
                    DefinitionJson.Options
                )!
            );
    }

    public async Task<T> ExecuteAsync<T>(
        Guid id,
        Func<ISessionTransaction, Task<T>> action,
        CancellationToken cancellationToken
    )
    {
        // The context owns only this operation, including changes discarded by a rollback.
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var rows = await db
            .GameSessions.FromSqlInterpolated(
                $"SELECT * FROM \"GameSessions\" WHERE \"Id\"={id} FOR UPDATE"
            )
            .ToListAsync(cancellationToken);
        var session =
            rows.SingleOrDefault() ?? throw new GameApplicationException("SessionNotFound", 404);
        var wallet = await db.DemoWallets.SingleAsync(x => x.SessionId == id, cancellationToken);
        var feature = await db.FeatureSessions.SingleAsync(
            x => x.SessionId == id,
            cancellationToken
        );
        var tx = new PostgresSessionTransaction(
            db,
            session,
            wallet,
            JsonSerializer.Deserialize<GameMathState>(feature.StateJson, DefinitionJson.Options)!
        );
        var result = await action(tx);
        feature.StateJson = JsonSerializer.Serialize(tx.State, DefinitionJson.Options);
        feature.Active = tx.State.IsBonus;
        feature.MathVersion = session.MathVersion;
        feature.Version = session.Version;
        feature.UpdatedAt = session.UpdatedAt;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<RoundRecord?> RoundAsync(
        Guid sessionId,
        Guid roundId,
        CancellationToken cancellationToken
    )
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db
            .RoundRecords.AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.SessionId == sessionId && r.Id == roundId,
                cancellationToken
            );
    }

    public async Task<RoundHistoryPage> HistoryPageAsync(
        Guid sessionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        // Both queries must observe the same committed rounds while other sessions keep writing.
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken
        );
        var items = await HistoryQuery(db, sessionId)
            .Skip(checked((page - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var total = await db.RoundRecords.LongCountAsync(
            r => r.SessionId == sessionId,
            cancellationToken
        );
        await transaction.CommitAsync(cancellationToken);
        return new(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<RoundHistoryItem>> HistoryAsync(
        Guid sessionId,
        int offset,
        int limit,
        CancellationToken cancellationToken
    )
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await HistoryQuery(db, sessionId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<RoundHistoryItem> HistoryQuery(GameDbContext db, Guid sessionId) =>
        db
            .RoundRecords.AsNoTracking()
            .Where(round => round.SessionId == sessionId)
            .OrderByDescending(round => round.CreatedAt)
            .ThenByDescending(round => round.Id)
            .Select(round => new RoundHistoryItem(
                round.Id,
                round.GameId,
                round.MathVersion,
                round.Mode,
                round.Stake,
                round.Award,
                round.CreatedAt
            ));
}
