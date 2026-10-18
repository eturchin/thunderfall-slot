using Thunderfall.Application.Errors;
using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Application.Services;

internal static class GameRegistryExtensions
{
    public static IGameMathModel ResolveModel(this IGameRegistry registry, string gameId, string? version = null)
    {
        try
        {
            return registry.Get(gameId, version);
        }
        catch (KeyNotFoundException)
        {
            throw new GameApplicationException("InvalidGame", 404);
        }
    }
}
