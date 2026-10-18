using Thunderfall.Api.Contracts;
using Thunderfall.Application;
using Thunderfall.Application.Contracts;
using Thunderfall.Application.Services;
using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.Contracts;
using static Thunderfall.Api.Security.SessionCredentials;

namespace Thunderfall.Api.Endpoints;

internal static class SessionEndpoints
{
    public static RouteGroupBuilder MapSessionEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/sessions", async (CreateSessionCommand request, SessionService service, HttpContext c, CancellationToken cancellationToken) =>
        {
            var (view, secret) = await service.CreateAsync(request.GameId, cancellationToken);
            c.Response.Cookies.Append("tf_session", view.Id.ToString("N") + "." + secret, new()
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = c.Request.IsHttps,
                Path = "/api/v1",
                MaxAge = TimeSpan.FromDays(30),
            });
            return Results.Created("/api/v1/sessions/current", view);
        }).Produces<SessionView>(201);
        api.MapGet("/sessions/current", async (SessionService s, HttpContext c, CancellationToken cancellationToken) =>
        {
            var auth = Credentials(c);
            return Results.Ok(await s.CurrentAsync(auth.Id, auth.Secret, cancellationToken));
        }).Produces<SessionView>();
        api.MapPost("/sessions/game", async (CreateSessionCommand request, SessionService s, HttpContext c, CancellationToken cancellationToken) =>
        {
            var auth = Credentials(c);
            return Results.Ok(await s.SwitchGameAsync(auth.Id, auth.Secret, request.GameId, cancellationToken));
        }).Produces<SessionView>();
        api.MapGet("/features/current", async (SessionService s, HttpContext c, CancellationToken cancellationToken) =>
        {
            var auth = Credentials(c);
            return Results.Ok((await s.CurrentAsync(auth.Id, auth.Secret, cancellationToken)).FeatureState);
        }).Produces<GameMathState>();
        api.MapPost("/sessions/reset", async (SessionService s, HttpContext c, CancellationToken cancellationToken) =>
        {
            var auth = Credentials(c);
            return Results.Ok(await s.ResetAsync(auth.Id, auth.Secret, cancellationToken));
        }).Produces<SessionView>();
        return api;
    }
}
