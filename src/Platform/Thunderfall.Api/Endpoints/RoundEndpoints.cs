using Thunderfall.Application.Contracts;
using Thunderfall.Application.Services;
using static Thunderfall.Api.Security.SessionCredentials;

namespace Thunderfall.Api.Endpoints;

internal static class RoundEndpoints
{
    public static RouteGroupBuilder MapRoundEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/rounds", async (RoundCommand request, RoundExecutionService service, HttpContext c, CancellationToken cancellationToken) =>
        {
            var auth = Credentials(c);
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            budget.CancelAfter(TimeSpan.FromSeconds(10));
            return Results.Ok(await service.ExecuteAsync(auth.Id, auth.Secret, request, budget.Token));
        }).Produces<RoundResponse>();
        api.MapGet("/rounds/{roundId:guid}", async (Guid roundId, SessionService s, HttpContext c, CancellationToken cancellationToken) =>
        {
            var auth = Credentials(c);
            return Results.Ok(await s.RoundAsync(auth.Id, auth.Secret, roundId, cancellationToken));
        }).Produces<RoundResponse>();
        api.MapGet("/rounds/history/page", async (int? page, int? pageSize, SessionService s, HttpContext c, CancellationToken cancellationToken) =>
        {
            var auth = Credentials(c);
            return Results.Ok(await s.HistoryPageAsync(auth.Id, auth.Secret, page ?? 1, pageSize ?? 20, cancellationToken));
        }).Produces<RoundHistoryPage>();
        api.MapGet("/rounds/history", async (int? offset, int? limit, SessionService s, HttpContext c, CancellationToken cancellationToken) =>
        {
            var auth = Credentials(c);
            return Results.Ok(await s.HistoryAsync(auth.Id, auth.Secret, offset ?? 0, limit ?? 20, cancellationToken));
        }).Produces<IReadOnlyList<RoundHistoryItem>>();
        return api;
    }
}
