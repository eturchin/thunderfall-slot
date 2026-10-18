using System.Net;
using System.Net.Http.Json;
using Thunderfall.Application.Contracts;

namespace Thunderfall.Api.IntegrationTests;

public sealed class HistoryTests(ApiFactory factory) : ApiTestBase(factory), IClassFixture<ApiFactory>
{
    [Fact]
    public async Task AllRoundsRemainPagedAfterResetAndGameSwitch()
    {
        using var c = Factory.Client();
        var original = await Create(c, "classic");
        var ids = new HashSet<Guid>();
        for (var i = 0; i < 43; i++)
        {
            var round = await Read<RoundResponse>(await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(1000, Guid.NewGuid())));
            ids.Add(round.RoundId);
        }

        var seen = new HashSet<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            var result = await Read<RoundHistoryPage>(await c.GetAsync($"/api/v1/rounds/history/page?page={page}&pageSize=20"));
            Assert.Equal(43, result.Total);
            Assert.Equal(page == 3 ? 3 : 20, result.Items.Count);
            foreach (var item in result.Items)
            {
                Assert.True(seen.Add(item.RoundId));
            }
        }

        Assert.True(ids.SetEquals(seen));
        var before = await Read<SessionView>(await c.GetAsync("/api/v1/sessions/current"));
        var switched = await Read<SessionView>(await c.PostAsJsonAsync("/api/v1/sessions/game", new
        {
            gameId = "thunderfall"
        }));
        Assert.Equal(original.Id, switched.Id);
        Assert.Equal(before.Balance, switched.Balance);
        await Read<SessionView>(await c.PostAsJsonAsync("/api/v1/sessions/reset", new
        {
        }));
        var retained = await Read<RoundHistoryPage>(await c.GetAsync("/api/v1/rounds/history/page?page=3"));
        Assert.Equal(43, retained.Total);
        Assert.Equal(3, retained.Items.Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/rounds/history/page?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/rounds/history/page?pageSize=51")).StatusCode);
        using var stranger = Factory.Client();
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync("/api/v1/rounds/history/page")).StatusCode);
    }

    [Fact]
    public async Task HistoryIsBoundedAndRoundIsolationHolds()
    {
        using var c = Factory.Client();
        await Create(c, "classic");
        var r = await Read<RoundResponse>(await c.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(1000, Guid.NewGuid())));
        var history = await Read<RoundHistoryItem[]>(await c.GetAsync("/api/v1/rounds/history?limit=999"));
        Assert.Single(history);
        Assert.Equal(r.RoundId, history[0].RoundId);
        using var stranger = Factory.Client();
        await Create(stranger);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync("/api/v1/rounds/" + r.RoundId)).StatusCode);
    }
}
