using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Thunderfall.Application.Entities;
using Thunderfall.Application.Persistence;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Infrastructure;

namespace Thunderfall.Api.IntegrationTests;

public sealed class SessionStoreTests(ApiFactory factory)
    : ApiTestBase(factory),
        IClassFixture<ApiFactory>
{
    [Fact]
    public async Task ReusedStoreReadsLatestCommittedStateUnderLock()
    {
        using var client = Factory.Client();
        var session = await Create(client);
        using var scope = Factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISessionStore>();
        await store.ExecuteAsync(
            session.Id,
            transaction => Task.FromResult(transaction.Balance),
            default
        );

        using (var otherScope = Factory.Services.CreateScope())
        {
            var otherStore = otherScope.ServiceProvider.GetRequiredService<ISessionStore>();
            await otherStore.ExecuteAsync(
                session.Id,
                transaction => Credit(transaction, 100),
                default
            );
        }

        var balance = await store.ExecuteAsync(
            session.Id,
            transaction => Credit(transaction, 200),
            default
        );

        Assert.Equal(session.Balance + 300, balance);
        await AssertReconciledAsync(session.Id, balance, 2, 3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOperationDoesNotLeakTrackedChangesIntoNextOperation(bool failDuringSave)
    {
        using var client = Factory.Client();
        var session = await Create(client);
        using var scope = Factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISessionStore>();

        Task<long> FailAsync() =>
            store.ExecuteAsync<long>(
                session.Id,
                transaction =>
                {
                    transaction.State = new GameMathState(
                        new(
                            Guid.NewGuid(),
                            session.GameId,
                            session.MathVersion,
                            1000,
                            5,
                            0,
                            1,
                            0,
                            0,
                            0,
                            0
                        )
                    );
                    var credit = Credit(transaction, -transaction.Balance - 1);
                    return failDuringSave
                        ? credit
                        : throw new InvalidOperationException("Injected failure");
                },
                default
            );

        if (failDuringSave)
        {
            await Assert.ThrowsAsync<DbUpdateException>(FailAsync);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(FailAsync);
        }

        await AssertReconciledAsync(session.Id, session.Balance, 0, 1);

        var balance = await store.ExecuteAsync(
            session.Id,
            transaction =>
            {
                Assert.Equal(session.Balance, transaction.Balance);
                Assert.Equal(0, transaction.Session.Version);
                Assert.Equal(GameMathState.Empty, transaction.State);
                return Credit(transaction, 100);
            },
            default
        );

        await AssertReconciledAsync(session.Id, balance, 1, 2);
    }

    private static Task<long> Credit(ISessionTransaction transaction, long delta)
    {
        var timestamp = DateTimeOffset.UtcNow;
        transaction.Balance = checked(transaction.Balance + delta);
        transaction.Session.Version = checked(transaction.Session.Version + 1);
        transaction.Session.UpdatedAt = timestamp;
        transaction.Add(
            new WalletEntry(
                Guid.NewGuid(),
                transaction.Session.Id,
                null,
                "TestAdjustment",
                delta,
                transaction.Balance,
                timestamp
            )
        );
        return Task.FromResult(transaction.Balance);
    }

    private async Task AssertReconciledAsync(
        Guid sessionId,
        long balance,
        long version,
        int entries
    )
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        Assert.Equal(
            balance,
            (await db.DemoWallets.SingleAsync(wallet => wallet.SessionId == sessionId)).Balance
        );
        Assert.Equal(
            balance,
            await db
                .WalletLedger.Where(entry => entry.SessionId == sessionId)
                .SumAsync(entry => entry.Delta)
        );
        Assert.Equal(
            entries,
            await db.WalletLedger.CountAsync(entry => entry.SessionId == sessionId)
        );
        Assert.Equal(
            version,
            (await db.GameSessions.SingleAsync(session => session.Id == sessionId)).Version
        );
        var feature = await db.FeatureSessions.SingleAsync(feature =>
            feature.SessionId == sessionId
        );
        Assert.False(feature.Active);
        Assert.Equal(version, feature.Version);
        Assert.False(await db.RoundRecords.AnyAsync(round => round.SessionId == sessionId));
        Assert.False(
            await db.IdempotencyRecords.AnyAsync(operation => operation.SessionId == sessionId)
        );
    }
}
