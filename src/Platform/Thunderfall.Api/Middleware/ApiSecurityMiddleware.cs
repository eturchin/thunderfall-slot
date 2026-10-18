using Thunderfall.Application.Errors;

namespace Thunderfall.Api.Middleware;

internal sealed class ApiSecurityMiddleware(RequestDelegate next, ILogger<ApiSecurityMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            if (HttpMethods.IsPost(context.Request.Method))
            {
                var valid = context.Request.HasJsonContentType() && context.Request.Headers["X-Thunderfall-Client"] == "web" && context.Request.Headers["Sec-Fetch-Site"] != "cross-site";
                if (context.Request.Headers.TryGetValue("Origin", out var origin))
                {
                    valid &= Uri.TryCreate(origin.ToString(), UriKind.Absolute, out var uri) && string.Equals(uri.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase) && uri.Scheme == context.Request.Scheme;
                }

                if (!valid)
                {
                    await Results.Problem(statusCode: 403, title: "InvalidOrigin").ExecuteAsync(context);
                    return;
                }
            }

            await next(context);
        }
        catch (GameApplicationException e)
        {
            await Results.Problem(statusCode: e.Status, title: e.Code, extensions: new Dictionary<string, object?> { { "code", e.Code } }).ExecuteAsync(context);
        }
        catch (KeyNotFoundException)
        {
            await Results.Problem(statusCode: 404, title: "InvalidGame", extensions: new Dictionary<string, object?> { { "code", "InvalidGame" } }).ExecuteAsync(context);
        }
        catch (OverflowException)
        {
            logger.LogError("Checked arithmetic rejected a round; transaction rolled back");
            await Results.Problem(statusCode: 422, title: "ArithmeticOverflow", extensions: new Dictionary<string, object?> { { "code", "ArithmeticOverflow" } }).ExecuteAsync(context);
        }
    }
}
