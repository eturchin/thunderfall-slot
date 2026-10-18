using Microsoft.EntityFrameworkCore;
using Thunderfall.Api.Configuration;
using Thunderfall.Api.Endpoints;
using Thunderfall.Api.Middleware;
using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Infrastructure;

var builder = WebApplication.CreateBuilder(args).AddThunderfall();
var app = builder.Build();
_ = app.Services.GetRequiredService<IGameRegistry>();
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.MigrateAsync();
    return;
}

app.UseExceptionHandler();
app.UseMiddleware<ApiSecurityMiddleware>();
app.UseRateLimiter();
app.MapGroup("/api/v1").RequireRateLimiting("api").MapGameEndpoints().MapSessionEndpoints().MapRoundEndpoints();
app.MapHealthEndpoints();
app.MapOpenApi();
app.Run();
