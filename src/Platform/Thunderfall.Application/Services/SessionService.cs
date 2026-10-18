using Thunderfall.Application.Contracts;
using Thunderfall.Application.Entities;
using Thunderfall.Application.Errors;
using Thunderfall.Application.Persistence;
using Thunderfall.Application.Security;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Application.Services;

public sealed class SessionService(ISessionStore store, IGameRegistry registry)
{
    public async Task<(SessionView View, string Secret)> CreateAsync(
        string gameId,
        CancellationToken cancellationToken
    )
    {
        var model = registry.ResolveModel(gameId);
        var definition = model.Configuration.Definition;
        var secret = SessionToken.New();
        var now = DateTimeOffset.UtcNow;
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            GameId = definition.GameId,
            MathVersion = definition.MathVersion,
            RulesetHash = model.Configuration.RulesetHash,
            TokenHash = SessionToken.Hash(secret),
            CreatedAt = now,
            UpdatedAt = now,
        };
        await store.CreateAsync(session, AmountMath.InitialBalance, cancellationToken);
        return (
            new(
                session.Id,
                definition.GameId,
                definition.MathVersion,
                session.RulesetHash,
                AmountMath.InitialBalance,
                GameMathState.Empty,
                0
            ),
            secret
        );
    }

    public async Task<SessionView> CurrentAsync(
        Guid id,
        string? secret,
        CancellationToken cancellationToken
    )
    {
        var snapshot = await store.ReadAsync(id, cancellationToken);
        SessionToken.Authorize(snapshot?.Session, secret);
        return View(snapshot!);
    }

    public async Task<RoundResponse> RoundAsync(
        Guid id,
        string? secret,
        Guid roundId,
        CancellationToken cancellationToken
    )
    {
        await CurrentAsync(id, secret, cancellationToken);
        var record =
            await store.RoundAsync(id, roundId, cancellationToken)
            ?? throw new GameApplicationException("RoundNotFound", 404);
        return RoundResponseSerializer.Deserialize(record.OutcomeJson);
    }

    public async Task<SessionView> SwitchGameAsync(
        Guid id,
        string? secret,
        string gameId,
        CancellationToken cancellationToken
    )
    {
        var model = registry.ResolveModel(gameId);
        return await store.ExecuteAsync(
            id,
            tx =>
            {
                SessionToken.Authorize(tx.Session, secret);
                if (tx.State.IsBonus)
                {
                    throw new GameApplicationException("BonusInProgress", 409);
                }

                var definition = model.Configuration.Definition;
                tx.Session.GameId = definition.GameId;
                tx.Session.MathVersion = definition.MathVersion;
                tx.Session.RulesetHash = model.Configuration.RulesetHash;
                tx.Session.UpdatedAt = DateTimeOffset.UtcNow;
                tx.Session.Version = checked(tx.Session.Version + 1);
                return Task.FromResult(
                    new SessionView(
                        tx.Session.Id,
                        definition.GameId,
                        definition.MathVersion,
                        model.Configuration.RulesetHash,
                        tx.Balance,
                        tx.State,
                        tx.Session.Version
                    )
                );
            },
            cancellationToken
        );
    }

    public async Task<RoundHistoryPage> HistoryPageAsync(
        Guid id,
        string? secret,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        if (page < 1 || pageSize < 1 || pageSize > 50 || (long)(page - 1) * pageSize > int.MaxValue)
        {
            throw new GameApplicationException("InvalidPagination");
        }

        await CurrentAsync(id, secret, cancellationToken);
        return await store.HistoryPageAsync(id, page, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<RoundHistoryItem>> HistoryAsync(
        Guid id,
        string? secret,
        int offset,
        int limit,
        CancellationToken cancellationToken
    )
    {
        await CurrentAsync(id, secret, cancellationToken);
        return await store.HistoryAsync(
            id,
            System.Math.Max(offset, 0),
            System.Math.Clamp(limit, 1, 50),
            cancellationToken
        );
    }

    public Task<SessionView> ResetAsync(
        Guid id,
        string? secret,
        CancellationToken cancellationToken
    ) =>
        store.ExecuteAsync(
            id,
            tx =>
            {
                SessionToken.Authorize(tx.Session, secret);
                var now = DateTimeOffset.UtcNow;
                tx.Add(
                    new WalletEntry(
                        Guid.NewGuid(),
                        id,
                        null,
                        "DemoReset",
                        checked(AmountMath.InitialBalance - tx.Balance),
                        AmountMath.InitialBalance,
                        now
                    )
                );
                tx.Balance = AmountMath.InitialBalance;
                tx.State = GameMathState.Empty;
                tx.Session.Version = checked(tx.Session.Version + 1);
                tx.Session.UpdatedAt = now;
                return Task.FromResult(View(new(tx.Session, tx.Balance, tx.State)));
            },
            cancellationToken
        );

    private static SessionView View(SessionSnapshot s) =>
        new(
            s.Session.Id,
            s.Session.GameId,
            s.Session.MathVersion,
            s.Session.RulesetHash,
            s.Balance,
            s.State,
            s.Session.Version
        );
}
