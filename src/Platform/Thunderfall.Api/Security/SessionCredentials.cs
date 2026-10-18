using Thunderfall.Application;
using Thunderfall.Application.Errors;

namespace Thunderfall.Api.Security;

internal static class SessionCredentials
{
    public static (Guid Id, string Secret) Credentials(HttpContext c)
    {
        var value = c.Request.Cookies["tf_session"];
        var parts = value?.Split('.', 2);
        if (parts?.Length != 2 || !Guid.TryParse(parts[0], out var id))
        {
            throw new GameApplicationException("SessionNotFound", 404);
        }

        return (id, parts[1]);
    }
}
