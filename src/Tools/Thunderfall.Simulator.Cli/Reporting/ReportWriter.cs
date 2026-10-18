using System.Globalization;
using System.Net;
using System.Text.Json;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Simulation.Simulation;

namespace Thunderfall.Simulator.Cli.Reporting;

public static class ReportWriter
{
    public static void Write(SimulationReport report, string directory, string label)
    {
        Directory.CreateDirectory(directory);
        var prefix = Path.Combine(directory, label);
        var element = JsonSerializer.SerializeToElement(report, DefinitionJson.Options);
        File.WriteAllText(
            prefix + ".json",
            JsonSerializer.Serialize(report, DefinitionJson.Options)
        );
        static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
        var scalars = element
            .EnumerateObject()
            .Where(p => p.Value.ValueKind is not JsonValueKind.Object and not JsonValueKind.Array)
            .ToArray();
        File.WriteAllText(
            prefix + ".csv",
            string.Join(',', scalars.Select(p => Csv(p.Name)))
                + "\n"
                + string.Join(',', scalars.Select(p => Csv(p.Value.ToString())))
                + "\n"
        );
        var rows = element
            .EnumerateObject()
            .Select(p =>
                $"<tr><th>{WebUtility.HtmlEncode(p.Name)}</th><td><pre>{WebUtility.HtmlEncode(p.Value.ToString())}</pre></td></tr>"
            );
        var html =
            "<!doctype html><html lang=en><meta charset=utf-8><meta name=viewport content='width=device-width'><title>Thunderfall · Math verification</title><style>body{background:#0a101b;color:#dce6f2;font:15px system-ui;max-width:1100px;margin:50px auto;padding:25px}h1{font:40px Georgia;color:#e4c48b}p{line-height:1.7;color:#9daac0}table{width:100%;border-collapse:collapse}th,td{padding:15px;border-bottom:1px solid #ffffff15;text-align:left}th{color:#ceb888}pre{white-space:pre-wrap;overflow-wrap:anywhere;font-size:13px}.metric{font:50px Georgia;color:#e7c27f}</style><h1>THUNDERFALL / Mathematical Report</h1><div class=metric>"
            + (report.ObservedRtp * 100).ToString("F5", CultureInfo.InvariantCulture)
            + "% RTP</div><p>Complete paid cycles including all free spins and retriggers. 95% approximate normal interval uses cycle-level variance. Sample estimates are not certification or an exact overall RTP proof.</p><table>"
            + string.Join("", rows)
            + "</table></html>";
        File.WriteAllText(prefix + ".html", html);
    }
}
