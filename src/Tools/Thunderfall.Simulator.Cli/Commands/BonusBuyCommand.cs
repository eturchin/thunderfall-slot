using System.Globalization;
using System.Text.Json;
using Thunderfall.Engine.Abstractions.BonusBuyPolicy;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Simulator.Cli.Commands;

internal static class BonusBuyCommand
{
    public static void Execute(IReadOnlyDictionary<string, string> options, IGameMathModel model, string directory)
    {
        var definition = model.Configuration.Definition;
        var cycles = long.Parse(options.GetValueOrDefault("rounds", "1000000"), CultureInfo.InvariantCulture);
        var seed = ulong.Parse(options.GetValueOrDefault("seed", "123456"), CultureInfo.InvariantCulture);
        var stream = ulong.Parse(options.GetValueOrDefault("stream", "0"), CultureInfo.InvariantCulture);
        var stake = definition.Stakes[0];
        var cost = BonusBuyPolicy.Price(stake);
        long total = 0;
        long bonusRounds = 0;
        double mean = 0;
        double m2 = 0;
        var rng = new Xoshiro256StarStar(0);
        var domain = Xoshiro256StarStar.Domain(definition.GameId, definition.MathVersion + ":" + BonusBuyPolicy.Version);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        for (long i = 0; i < cycles; i++)
        {
            rng.Reset(Xoshiro256StarStar.CycleSeed(seed, stream, i, domain));
            var state = BonusBuyPolicy.Enter(model.Configuration, stake, Guid.Empty);
            long award = 0;
            var work = 0;
            do
            {
                if (++work > definition.ResourceLimits.MaxBonusRounds)
                {
                    throw new InvalidOperationException("Bonus work exhausted");
                }

                var result = model.ExecuteSummary(new(stake, Guid.Empty), state, rng);
                state = result.State;
                award = checked(award + result.Award);
                bonusRounds++;
            }
            while (state.IsBonus);
            total = checked(total + award);
            var x = (double)award / cost;
            var delta = x - mean;
            mean += delta / (i + 1);
            m2 += delta * (x - mean);
        }

        var se = System.Math.Sqrt(m2 / (cycles - 1) / cycles);
        var report = new
        {
            policyVersion = BonusBuyPolicy.Version,
            gameId = definition.GameId,
            definition.MathVersion,
            rulesetHash = model.Configuration.RulesetHash,
            paidPurchases = cycles,
            seed,
            stream,
            stake,
            cost,
            totalCost = checked(cost * cycles),
            totalAward = total,
            bonusRounds,
            observedRtp = (double)total / (cost * cycles),
            confidenceInterval95 = new[]
            {
                mean - 1.96 * se,
                mean + 1.96 * se
            },
            standardError = se,
            elapsedSeconds = timer.Elapsed.TotalSeconds,
        };
        var label = options.GetValueOrDefault("label", "bonus-buy");
        var json = JsonSerializer.Serialize(report, DefinitionJson.Options);
        File.WriteAllText(Path.Combine(directory, label + ".json"), json);
        File.WriteAllText(Path.Combine(directory, label + ".html"), "<!doctype html><meta charset=utf-8><title>Bonus Buy verification</title><h1>Separate Bonus Buy RTP</h1><p>Denominator: purchase cost. All retriggers included. Approximate cycle-level normal confidence interval.</p><pre>" + System.Net.WebUtility.HtmlEncode(json) + "</pre>");
        Console.WriteLine(json);
        return;
    }
}
