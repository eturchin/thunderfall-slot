using System.Text.Json;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Simulation.Analysis;

namespace Thunderfall.Simulator.Cli.Commands;

internal static class AnalyticalCommand
{
    public static void Execute(IReadOnlyDictionary<string, string> options, IGameMathModel model, string directory)
    {
        var definition = model.Configuration.Definition;
        var exact = ExactProbability.Analyze(definition);
        File.WriteAllText(Path.Combine(directory, "initial-board-exact.json"), JsonSerializer.Serialize(exact, DefinitionJson.Options));
        File.WriteAllText(Path.Combine(directory, "bonus-initial-board-exact.json"), JsonSerializer.Serialize(ExactProbability.Analyze(definition, true), DefinitionJson.Options));
        Console.WriteLine("Exact initial-board report written; no claim of exact full-cycle RTP.");
        return;
    }
}
