using System.Net;
using Thunderfall.Application.Contracts;

namespace Thunderfall.Api.IntegrationTests;

public sealed class SessionSecurityTests(ApiFactory factory) : ApiTestBase(factory), IClassFixture<ApiFactory>
{
    [Fact]
    public async Task CookieProtectsSession()
    {
        using var c = Factory.Client();
        var s = await Create(c);
        using var stranger = Factory.Client();
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync("/api/v1/sessions/current")).StatusCode);
        Assert.Equal(s.Id, (await Read<SessionView>(await c.GetAsync("/api/v1/sessions/current"))).Id);
    }
}
