using System.Globalization;
using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Simulation;
using Thunderfall.Engine.Simulation.Simulation;
using Thunderfall.Simulator.Cli.Reporting;

namespace Thunderfall.Simulator.Cli.Commands;

internal static class PaidCycleCommand
{
    public static void Execute(IReadOnlyDictionary<string, string> options, IGameMathModel model, string directory)
    {
        var definition = model.Configuration.Definition;
        var game = definition.GameId;
        var seeds = options.GetValueOrDefault("seeds", options.GetValueOrDefault("seed", "123456")).Split(',');
        foreach (var seedText in seeds)
        {
            var seed = ulong.Parse(seedText, CultureInfo.InvariantCulture);
            var settings = new SimulationOptions(long.Parse(options.GetValueOrDefault("rounds", "1000000"), CultureInfo.InvariantCulture), seed, ulong.Parse(options.GetValueOrDefault("stream", "0"), CultureInfo.InvariantCulture), int.Parse(options.GetValueOrDefault("workers", "1"), CultureInfo.InvariantCulture));
            Console.WriteLine($"{definition.GameId}@{definition.MathVersion} cycles={settings.PaidCycles} seed={seed} workers={settings.Workers}");
            var report = new SimulationRunner().Run(model, settings);
            ReportWriter.Write(report, directory, options.GetValueOrDefault("label", $"{game}-{seed}"));
            Console.WriteLine($"RTP={report.ObservedRtp:P6} 95%CI=[{report.ConfidenceInterval95.Lower:P6},{report.ConfidenceInterval95.Upper:P6}] base={report.BaseRtpContribution:P4} bonus={report.BonusRtpContribution:P4} cycles/s={report.CyclesPerSecond:F0} allocated={report.AllocatedBytes} capHits={report.MaxWinHits}");
        }
    }
}
