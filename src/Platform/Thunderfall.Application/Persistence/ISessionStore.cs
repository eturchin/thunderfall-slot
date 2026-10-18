using Thunderfall.Application.Contracts;
using Thunderfall.Application.Entities;

namespace Thunderfall.Application.Persistence;

public interface ISessionStore
{
    Task<RoundHistoryPage> HistoryPageAsync(
        Guid sessionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
    Task CreateAsync(GameSession session, long balance, CancellationToken cancellationToken);
    Task<SessionSnapshot?> ReadAsync(Guid id, CancellationToken cancellationToken);
    Task<T> ExecuteAsync<T>(
        Guid id,
        Func<ISessionTransaction, Task<T>> action,
        CancellationToken cancellationToken
    );
    Task<RoundRecord?> RoundAsync(
        Guid sessionId,
        Guid roundId,
        CancellationToken cancellationToken
    );
    Task<IReadOnlyList<RoundHistoryItem>> HistoryAsync(
        Guid sessionId,
        int offset,
        int limit,
        CancellationToken cancellationToken
    );
}
