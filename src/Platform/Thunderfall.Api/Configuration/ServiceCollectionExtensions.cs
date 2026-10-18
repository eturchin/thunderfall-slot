using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Thunderfall.Application.Persistence;
using Thunderfall.Application.Services;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Randomness;
using Thunderfall.Games.Classic;
using Thunderfall.Games.Storm;
using Thunderfall.Infrastructure;

namespace Thunderfall.Api.Configuration;

internal static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddThunderfall(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 16 * 1024);
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            );
            o.SerializerOptions.UnmappedMemberHandling = System
                .Text
                .Json
                .Serialization
                .JsonUnmappedMemberHandling
                .Disallow;
        });
        builder.Services.AddSingleton<IGameRegistry>(_ => new GameRegistry(
            new IGameMathModel[]
            {
                StormModule.Load(
                    Path.Combine(AppContext.BaseDirectory, "configs/thunderfall/v1/game.json")
                ),
                ClassicModule.Load(
                    Path.Combine(AppContext.BaseDirectory, "configs/classic/v1/game.json")
                ),
            }
        ));
        builder.Services.AddScoped<IRandomSource, CryptoRandomSource>();
        builder.Services.AddScoped<SessionService>();
        builder.Services.AddScoped<RoundExecutionService>();
        builder.Services.AddScoped<ISessionStore, PostgresSessionStore>();
        builder.Services.AddDbContextFactory<GameDbContext>(o =>
            o.UseNpgsql(
                builder.Configuration.GetConnectionString("Game")
                    ?? throw new InvalidOperationException("Set ConnectionStrings__Game")
            )
        );
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.AddPolicy(
                "api",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "local",
                        _ =>
                            new()
                            {
                                PermitLimit = 240,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                            }
                    )
            );
        });
        return builder;
    }
}
