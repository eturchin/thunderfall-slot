using Thunderfall.Infrastructure;

namespace Thunderfall.Api.Endpoints;

internal static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
        app.MapGet("/health/ready", async (GameDbContext db, CancellationToken cancellationToken) => await db.Database.CanConnectAsync(cancellationToken) ? Results.Ok(new { status = "ready" }) : Results.Problem(statusCode: 503, title: "DatabaseUnavailable"));
        return app;
    }
}
