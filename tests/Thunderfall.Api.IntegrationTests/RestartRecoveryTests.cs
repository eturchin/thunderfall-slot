using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Thunderfall.Application.Contracts;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Randomness;
using Thunderfall.Infrastructure;
using Thunderfall.Tests;

namespace Thunderfall.Api.IntegrationTests;

public sealed class RestartRecoveryTests(ApiFactory factory) : ApiTestBase(factory), IClassFixture<ApiFactory>
{
    [Fact]
    public async Task PersistedBonusSurvivesActualApiProcessRestart()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var url = "http://127.0.0.1:" + port;
        var cookies = new CookieContainer();
        using var client = new HttpClient(new HttpClientHandler { CookieContainer = cookies, UseProxy = false })
        {
            BaseAddress = new Uri(url),
        };
        client.DefaultRequestHeaders.Add("X-Thunderfall-Client", "web");
        async Task<System.Diagnostics.Process> Start()
        {
            var info = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            info.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Thunderfall.Api.dll"));
            info.ArgumentList.Add("--urls");
            info.ArgumentList.Add(url);
            info.Environment["ConnectionStrings__Game"] = Factory.TestConnection;
            var process = System.Diagnostics.Process.Start(info)!;
            process.OutputDataReceived += (_, _) =>
            {
            };
            process.ErrorDataReceived += (_, _) =>
            {
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            for (var i = 0; i < 100; i++)
            {
                if (process.HasExited)
                {
                    throw new InvalidOperationException("API test process exited");
                }

                try
                {
                    if ((await client.GetAsync("/health/live")).IsSuccessStatusCode)
                    {
                        return process;
                    }
                }
                catch (HttpRequestException)
                {
                }

                await Task.Delay(100);
            }

            process.Kill(true);
            throw new TimeoutException("API did not become ready");
        }

        System.Diagnostics.Process? child = null;
        try
        {
            child = await Start();
            var session = await Create(client);
            var model = Fixtures.Model();
            var rng = new Xoshiro256StarStar(Xoshiro256StarStar.CycleSeed(42, 0, 628, Xoshiro256StarStar.Domain(session.GameId, session.MathVersion)));
            var trigger = model.Execute(new(1000, Guid.NewGuid()), GameMathState.Empty, rng);
            Assert.True(trigger.FinalMathState.IsBonus);
            using (var scope = Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
                var feature = await db.FeatureSessions.SingleAsync(f => f.SessionId == session.Id);
                feature.StateJson = Fixtures.Json(trigger.FinalMathState);
                feature.Active = true;
                await db.SaveChangesAsync();
            }

            var before = await Read<SessionView>(await client.GetAsync("/api/v1/sessions/current"));
            child.Kill(true);
            await child.WaitForExitAsync();
            child.Dispose();
            child = await Start();
            var after = await Read<SessionView>(await client.GetAsync("/api/v1/sessions/current"));
            Assert.Equal(before, after);
            var result = await Read<RoundResponse>(await client.PostAsJsonAsync("/api/v1/rounds", new RoundCommand(null, Guid.NewGuid())));
            Assert.Equal("FreeSpin", result.Mode);
            Assert.Equal(after.Balance + result.FinalWin, result.BalanceAfter);
        }
        finally
        {
            if (child is not null)
            {
                if (!child.HasExited)
                {
                    child.Kill(true);
                    await child.WaitForExitAsync();
                }

                child.Dispose();
            }
        }
    }
}
