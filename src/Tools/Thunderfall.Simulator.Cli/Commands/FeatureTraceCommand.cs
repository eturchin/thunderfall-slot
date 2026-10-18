using System.Globalization;
using System.Text.Json;
using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Simulator.Cli.Commands;

internal static class FeatureTraceCommand
{
    public static void Execute(IReadOnlyDictionary<string, string> options, IGameMathModel model, string directory)
    {
        var definition = model.Configuration.Definition;
        var traceSeed = ulong.Parse(options.GetValueOrDefault("seed", "42"), CultureInfo.InvariantCulture);
        var rng = new Xoshiro256StarStar(0);
        var domain = Xoshiro256StarStar.Domain(definition.GameId, definition.MathVersion);
        for (long index = 0; index < 100000; index++)
        {
            var cycleSeed = Xoshiro256StarStar.CycleSeed(traceSeed, 0, index, domain);
            rng.Reset(cycleSeed);
            var request = new RoundRequest(definition.Stakes[0], new Guid((int)index, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
            if (!model.ExecuteSummary(request, GameMathState.Empty, rng).Triggered)
            {
                continue;
            }

            rng.Reset(cycleSeed);
            var rounds = new List<RoundOutcome>();
            var round = model.Execute(request, GameMathState.Empty, rng);
            rounds.Add(round);
            var state = round.FinalMathState;
            var number = 1;
            while (state.IsBonus)
            {
                round = model.Execute(new(request.Stake, new Guid((int)index, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)number++)), state, rng);
                rounds.Add(round);
                state = round.FinalMathState;
            }

            File.WriteAllText(Path.Combine(directory, "engine-bonus-cycle.json"), JsonSerializer.Serialize(new
            {
                definition.GameId,
                definition.MathVersion,
                model.Configuration.RulesetHash,
                Seed = traceSeed,
                CycleIndex = index,
                Rounds = rounds,
            }, DefinitionJson.Options));
            Console.WriteLine($"Recorded actual engine cycle {index}, {rounds.Count} rounds");
            return;
        }

        throw new InvalidOperationException("No trigger found within trace search budget");
    }
}
