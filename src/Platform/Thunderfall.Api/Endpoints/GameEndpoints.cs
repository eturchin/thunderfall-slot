using Thunderfall.Api.Contracts;
using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.BonusBuyPolicy;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;

namespace Thunderfall.Api.Endpoints;

internal static class GameEndpoints
{
    public static RouteGroupBuilder MapGameEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/games", (IGameRegistry r) => r.Games.GroupBy(m => m.Configuration.Definition.GameId).Select(g => Info(r.Get(g.Key).Configuration)));
        api.MapGet("/games/{gameId}", (string gameId, string? version, IGameRegistry r) => Info(r.Get(gameId, version).Configuration));
        api.MapGet("/games/{gameId}/rules", (string gameId, string? version, IGameRegistry r) =>
        {
            var c = r.Get(gameId, version).Configuration;
            var definition = c.Definition;
            return new GameRules(definition.GameId, definition.MathVersion, c.RulesetHash, definition.Kind, definition.Layout, definition.Paytable, definition.FeatureRules, definition.MaxWinMultiplier, 100, 1_000_000, definition.GameId == "thunderfall" && definition.MathVersion == "1.0.0" ? new BonusBuyOptions(BonusBuyPolicy.Version, BonusBuyPolicy.PriceNumerator, BonusBuyPolicy.PriceDenominator, definition.FeatureRules.InitialSpins) : null);
        });
        return api;
    }

    private static GameInfo Info(CompiledDefinition c)
    {
        var definition = c.Definition;
        return new(definition.GameId, definition.MathVersion, c.RulesetHash, definition.Name, definition.Kind, definition.Layout, definition.Stakes, definition.Symbols);
    }
}
