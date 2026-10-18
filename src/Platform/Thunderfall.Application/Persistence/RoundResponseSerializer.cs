using System.Text;
using System.Text.Json;
using Thunderfall.Application.Contracts;
using Thunderfall.Application.Entities;
using Thunderfall.Application.Errors;
using Thunderfall.Engine.Abstractions.Definitions;

namespace Thunderfall.Application.Persistence;

internal static class RoundResponseSerializer
{
    private const int MaximumResponseBytes = 8 * 1024 * 1024;

    public static RoundResponse Deserialize(string json) =>
        JsonSerializer.Deserialize<RoundResponse>(json, DefinitionJson.Options)!;

    public static void Persist(
        ISessionTransaction transaction,
        Guid idempotencyKey,
        string fingerprint,
        RoundResponse response,
        DateTimeOffset timestamp)
    {
        var json = JsonSerializer.Serialize(response, DefinitionJson.Options);
        if (Encoding.UTF8.GetByteCount(json) > MaximumResponseBytes)
        {
            throw new GameApplicationException("OutcomeResourceLimit", 422);
        }

        transaction.Add(new RoundRecord(
            response.RoundId,
            transaction.Session.Id,
            response.GameId,
            response.MathVersion,
            response.Outcome.RulesetHash,
            response.Stake,
            response.FinalWin,
            response.Mode,
            json,
            timestamp));
        transaction.Add(new IdempotencyRecord(transaction.Session.Id, idempotencyKey, fingerprint, response.RoundId, timestamp));
    }
}
