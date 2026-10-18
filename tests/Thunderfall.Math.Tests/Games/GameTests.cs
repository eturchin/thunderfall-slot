using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Randomness;
using Thunderfall.Engine.Runtime;
using Thunderfall.Games.Classic;
using Thunderfall.Games.Storm;
using Thunderfall.Tests;

namespace Thunderfall.Math.Tests.Games;

public sealed class GameTests
{
    [Fact]
    public void BothModulesUseRegistryAndGenericPipeline()
    {
        var storm = StormModule.Load(
            Path.Combine(AppContext.BaseDirectory, "configs/thunderfall/v1/game.json")
        );
        var classic = ClassicModule.Load(
            Path.Combine(AppContext.BaseDirectory, "configs/classic/v1/game.json")
        );
        var registry = new GameRegistry([storm, classic]);
        foreach (var m in registry.Games)
        {
            var r = m.Execute(
                new(1000, Guid.Empty),
                GameMathState.Empty,
                new Xoshiro256StarStar(42)
            );
            ReplayValidator.Validate(m.Configuration, r);
            Assert.Equal(m.Configuration.Definition.Layout.Size, r.FinalBoard.Cells.Length);
        }
        Assert.Equal(2, registry.Games.Count);
        Assert.Throws<KeyNotFoundException>(() => registry.Get("missing"));
    }

    [Fact]
    public void WholeBonusSeriesPersistsAndNeverExceedsCycleCap()
    {
        var m = Fixtures.Model();
        var rng = new Xoshiro256StarStar(195);
        long checkedBonuses = 0;
        for (var cycle = 0; cycle < 5000; cycle++)
        {
            var round = m.Execute(
                new(1000, new Guid(cycle, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)),
                GameMathState.Empty,
                rng
            );
            var award = round.FinalWin;
            var state = round.FinalMathState;
            if (state.IsBonus)
            {
                checkedBonuses++;
            }

            while (state.IsBonus)
            {
                var json = Fixtures.Json(state);
                state = System.Text.Json.JsonSerializer.Deserialize<GameMathState>(
                    json,
                    DefinitionJson.Options
                )!;
                round = m.Execute(new(1000, Guid.Empty), state, rng);
                ReplayValidator.Validate(m.Configuration, round);
                state = round.FinalMathState;
                award += round.FinalWin;
                Assert.True(award <= 5_000_000);
            }
            Assert.True(award <= 5_000_000);
        }
        Assert.True(checkedBonuses > 0);
    }

    [Fact]
    public void ClassicStopsProduceActualCyclicRows()
    {
        var m = ClassicModule.Load(
            Path.Combine(AppContext.BaseDirectory, "configs/classic/v1/game.json")
        );
        var r = m.Execute(new(1000, Guid.Empty), GameMathState.Empty, new Xoshiro256StarStar(7));
        for (var c = 0; c < 5; c++)
        {
            for (var row = 0; row < 3; row++)
            {
                var strip = m.Configuration.Definition.ReelStrips[c];
                Assert.Equal(
                    strip[(r.ReelStops[c] + row) % strip.Length],
                    r.InitialBoard[c, row].Symbol
                );
            }
        }

        Assert.Empty(r.CascadeSteps);
        Assert.False(r.FinalMathState.IsBonus);
    }

    [Fact]
    public void RegistryResolvesSemanticVersionsAndKeepsHistoricVersion()
    {
        var definition = Fixtures.Definition();
        var old = new RoundEngine(definition with
        {
            MathVersion = "1.2.0"
        });
        var current = new RoundEngine(definition with
        {
            MathVersion = "1.10.0"
        });
        var registry = new GameRegistry([old, current]);
        Assert.Same(current, registry.Get(definition.GameId));
        Assert.Same(old, registry.Get(definition.GameId, "1.2.0"));
    }
}
