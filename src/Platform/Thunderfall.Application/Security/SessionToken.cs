using System.Security.Cryptography;
using System.Text;
using Thunderfall.Application.Entities;
using Thunderfall.Application.Errors;

namespace Thunderfall.Application.Security;

public static class SessionToken
{
    public static string New() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static void Authorize(GameSession? session, string? secret)
    {
        if (
            session is null
            || secret?.Length != 64
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(session.TokenHash),
                Encoding.ASCII.GetBytes(Hash(secret))
            )
        )
        {
            throw new GameApplicationException("SessionNotFound", 404);
        }
    }
}
