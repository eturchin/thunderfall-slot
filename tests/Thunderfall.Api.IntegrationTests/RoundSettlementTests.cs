using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thunderfall.Application;
using Thunderfall.Application.Contracts;
using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;
using Thunderfall.Engine.Runtime;
using Thunderfall.Infrastructure;
using Thunderfall.Tests;

namespace Thunderfall.Api.IntegrationTests;

public sealed class RoundSettlementTests(ApiFactory factory) : ApiTestBase(factory), IClassFixture<ApiFactory>
{
    [Fact]
    public async Task RoundSettlesAndReplays()
    {
        using var c = Factory.Client();
        var s = await Create(c);
        var r = await Read<RoundResponse>(await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(2000, Guid.NewGuid())));
        Assert.Equal(s.Balance - 2000 + r.FinalWin, r.BalanceAfter);
        using var scope = Factory.Services.CreateScope();
        ReplayValidator.Validate(scope.ServiceProvider.GetRequiredService<IGameRegistry>().Get(s.GameId, s.MathVersion).Configuration, r.Outcome);
        var stored = await Read<RoundResponse>(await c.GetAsync("/api/v1/rounds/" + r.RoundId));
        Assert.Equal(r.FinalWin, stored.FinalWin);
        Assert.Equal(r.Outcome.MathematicalEvents.Length, stored.Outcome.MathematicalEvents.Length);
    }

    [Fact]
    public async Task SameKeyReturnsSavedOutcomeAndRejectsChangedPayload()
    {
        using var c = Factory.Client();
        await Create(c);
        var body = new RoundCommand(2000, Guid.NewGuid());
        var first = await Read<RoundResponse>(await c.PostAsJsonAsync("/api/v1/rounds", body));
        var second = await Read<RoundResponse>(await c.PostAsJsonAsync("/api/v1/rounds", body));
        Assert.Equal(first.RoundId, second.RoundId);
        Assert.Equal(Fixtures.Json(first), Fixtures.Json(second));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/v1/rounds", body with
        {
            Stake = 5000
        })).StatusCode);
    }

    [Fact]
    public async Task ConcurrentIdenticalRequestsCommitOnce()
    {
        using var c = Factory.Client();
        var s = await Create(c);
        var body = new RoundCommand(1000, Guid.NewGuid());
        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => c.PostAsJsonAsync("/api/v1/rounds", body)));
        var rounds = await Task.WhenAll(responses.Select(Read<RoundResponse>));
        Assert.Single(rounds.Select(r => r.RoundId).Distinct());
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        Assert.Equal(1, await db.RoundRecords.CountAsync(r => r.SessionId == s.Id));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(r => r.SessionId == s.Id));
        Assert.Equal(s.Balance - 1000 + rounds[0].FinalWin, (await db.DemoWallets.SingleAsync(w => w.SessionId == s.Id)).Balance);
    }

    [Fact]
    public async Task ConcurrentDistinctRequestsReconcileLedger()
    {
        using var c = Factory.Client();
        var s = await Create(c);
        var responses = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(1000, Guid.NewGuid()))));
        Assert.All(responses, r => r.EnsureSuccessStatusCode());
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var balance = (await db.DemoWallets.SingleAsync(w => w.SessionId == s.Id)).Balance;
        Assert.Equal(balance, await db.WalletLedger.Where(w => w.SessionId == s.Id).SumAsync(w => w.Delta));
        Assert.Equal(16, await db.RoundRecords.CountAsync(r => r.SessionId == s.Id));
    }

    [Fact]
    public async Task InvalidStakeAndInsufficientFundsDoNotCommit()
    {
        using var c = Factory.Client();
        var s = await Create(c);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(3, Guid.NewGuid()))).StatusCode);
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var wallet = await db.DemoWallets.SingleAsync(w => w.SessionId == s.Id);
            wallet.Balance = 0;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(1000, Guid.NewGuid()))).StatusCode);
        using var check = Factory.Services.CreateScope();
        Assert.Equal(0, await check.ServiceProvider.GetRequiredService<GameDbContext>().RoundRecords.CountAsync(r => r.SessionId == s.Id));
    }

    [Fact]
    public async Task ResetHasLedgerEntryAndClearsFeature()
    {
        using var c = Factory.Client();
        var s = await Create(c);
        await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(1000, Guid.NewGuid()));
        var reset = await Read<SessionView>(await c.PostAsJsonAsync("/api/v1/sessions/reset", new
        {
        }));
        Assert.Equal(AmountMath.InitialBalance, reset.Balance);
        Assert.False(reset.FeatureState.IsBonus);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        Assert.True(await db.WalletLedger.AnyAsync(w => w.SessionId == s.Id && w.Type == "DemoReset"));
        Assert.Equal(reset.Balance, await db.WalletLedger.Where(w => w.SessionId == s.Id).SumAsync(w => w.Delta));
    }

    [Fact]
    public async Task FeatureSurvivesFreshDbContextAndFreeRoundHasNoStakeDebit()
    {
        using var c = Factory.Client();
        var s = await Create(c);
        var feature = new GameMathState(new(Guid.NewGuid(), s.GameId, s.MathVersion, 2000, 5, 2, 7, 100, 1, 1, 100));
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var row = await db.FeatureSessions.SingleAsync(f => f.SessionId == s.Id);
            row.StateJson = Fixtures.Json(feature);
            row.Active = true;
            await db.SaveChangesAsync();
        }

        var restored = await Read<SessionView>(await c.GetAsync("/api/v1/sessions/current"));
        Assert.Equal(feature, restored.FeatureState);
        var r = await Read<RoundResponse>(await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(null, Guid.NewGuid())));
        Assert.Equal("FreeSpin", r.Mode);
        Assert.Equal(s.Balance + r.FinalWin, r.BalanceAfter);
        Assert.True(r.FeatureState.Bonus!.PersistentMultiplier >= 7);
        using var check = Factory.Services.CreateScope();
        Assert.False(await check.ServiceProvider.GetRequiredService<GameDbContext>().WalletLedger.AnyAsync(w => w.RoundId == r.RoundId && w.Type == "Stake"));
    }

    [Fact]
    public async Task CsrfAndOpenApi()
    {
        using var c = Factory.Client();
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/health/ready")).StatusCode);
        var schema = await c.GetStringAsync("/openapi/v1.json");
        Assert.Contains("RoundResponse", schema);
        Assert.Contains("RoundCommand", schema);
        using var plain = Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await plain.PostAsJsonAsync("/api/v1/sessions", new
        {
            gameId = "thunderfall"
        })).StatusCode);
        c.DefaultRequestHeaders.Add("Origin", "https://unrelated.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/v1/sessions", new
        {
            gameId = "thunderfall"
        })).StatusCode);
    }

    [Fact]
    public async Task WorkExhaustionRollsBackEveryAccountingWrite()
    {
        var definition = Fixtures.Definition() with
        {
            MathVersion = "1.0.0-work-budget",
            ResourceLimits = new(1, 100000),
        };
        using var host = Factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.RemoveAll<IGameRegistry>();
            services.AddSingleton<IGameRegistry>(new GameRegistry([new RoundEngine(definition, new Fixtures.FixedGenerator(Fixtures.WinningBoard())),]));
            services.RemoveAll<IRandomSource>();
            services.AddScoped<IRandomSource>(_ => new Fixtures.RefillRandom(8));
        }));
        using var c = host.CreateClient();
        c.DefaultRequestHeaders.Add("X-Thunderfall-Client", "web");
        var session = await Create(c);
        var response = await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(1000, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        Assert.Equal(session.Balance, (await db.DemoWallets.SingleAsync(w => w.SessionId == session.Id)).Balance);
        Assert.Equal(1, await db.WalletLedger.CountAsync(w => w.SessionId == session.Id));
        Assert.False(await db.RoundRecords.AnyAsync(r => r.SessionId == session.Id));
        Assert.False(await db.IdempotencyRecords.AnyAsync(r => r.SessionId == session.Id));
    }
}
