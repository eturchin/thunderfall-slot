namespace Thunderfall.Application.Contracts;

public sealed record RoundHistoryPage(
    IReadOnlyList<RoundHistoryItem> Items,
    long Total,
    int Page,
    int PageSize
);
