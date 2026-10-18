using System.Net;
using System.Net.Http.Json;
using Thunderfall.Application;
using Thunderfall.Application.Contracts;

namespace Thunderfall.Api.IntegrationTests;

public sealed class BonusBuyTests(ApiFactory factory) : ApiTestBase(factory), IClassFixture<ApiFactory>
{
    [Fact]
    public async Task BonusBuyIsAtomicIdempotentAndResumable()
    {
        using var c = Factory.Client();
        var session = await Create(c);
        var key = Guid.NewGuid();
        var command = new RoundCommand(1000, key, true);
        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => c.PostAsJsonAsync("/api/v1/rounds", command)));
        var rounds = new List<RoundResponse>();
        foreach (var response in responses)
        {
            rounds.Add(await Read<RoundResponse>(response));
        }

        var r = rounds[0];
        Assert.All(rounds, x => Assert.Equal(r.RoundId, x.RoundId));
        Assert.Equal("BonusBuy", r.Mode);
        Assert.Equal(45500, r.BonusPurchase!.Cost);
        Assert.Equal(15, r.BonusPurchase.AwardedSpins);
        Assert.Equal(session.Balance - 45500 + r.FinalWin, r.BalanceAfter);
        Assert.Equal(1, r.FeatureState.Bonus!.PlayedSpins);
        Assert.True(r.FeatureState.IsBonus);
        var current = await Read<SessionView>(await c.GetAsync("/api/v1/sessions/current"));
        Assert.Equal(r.FeatureState, current.FeatureState);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(1000, key))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(1000, Guid.NewGuid(), true))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/v1/sessions/game", new
        {
            gameId = "classic"
        })).StatusCode);
        var page = await Read<RoundHistoryPage>(await c.GetAsync("/api/v1/rounds/history/page"));
        Assert.Equal(1, page.Total);
        Assert.Equal("BonusBuy", page.Items[0].Mode);
        await Read<RoundResponse>(await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(null, Guid.NewGuid())));
    }
}
