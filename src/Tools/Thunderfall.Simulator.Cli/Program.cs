using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Games.Classic;
using Thunderfall.Games.Storm;
using Thunderfall.Simulator.Cli.Commands;

var options = new Dictionary<string, string>();
for (var i = 0; i < args.Length; i += 2)
{
    if (i + 1 >= args.Length || !args[i].StartsWith("--", StringComparison.Ordinal))
    {
        throw new ArgumentException("Use --option value");
    }

    options[args[i][2..]] = args[i + 1];
}

var game = options.GetValueOrDefault("game", "thunderfall");

var path = options.GetValueOrDefault("config", Path.Combine(AppContext.BaseDirectory, "configs", game, "v1", "game.json"));

var directory = options.GetValueOrDefault("out", "reports");
Directory.CreateDirectory(directory);
var definition = DefinitionJson.Load(path);
var model = definition.Kind == "Paylines" ? ClassicModule.Load(path) : StormModule.Load(path);
if (options.GetValueOrDefault("bonus-buy", "false") == "true")
{
    BonusBuyCommand.Execute(options, model, directory);
    return;
}

if (options.GetValueOrDefault("analytical", "false") == "true")
{
    AnalyticalCommand.Execute(options, model, directory);
    return;
}

if (options.GetValueOrDefault("trace-feature", "false") == "true")
{
    FeatureTraceCommand.Execute(options, model, directory);
    return;
}

PaidCycleCommand.Execute(options, model, directory);
