using System.Net.Http.Json;
using System.Text.Json;
using Thunderfall.Application;
using Thunderfall.Application.Contracts;
using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.Definitions;

namespace Thunderfall.Api.IntegrationTests;

public abstract class ApiTestBase(ApiFactory factory)
{
    protected ApiFactory Factory { get; } = factory;

    protected static async Task<T> Read<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), DefinitionJson.Options)!;
    }

    protected static async Task<SessionView> Create(HttpClient c, string game = "thunderfall") => await Read<SessionView>(await c.PostAsJsonAsync("/api/v1/sessions", new { gameId = game }));
}
