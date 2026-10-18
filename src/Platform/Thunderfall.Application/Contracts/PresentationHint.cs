namespace Thunderfall.Application.Contracts;

public sealed record PresentationHint(
    string Type,
    int? Step = null,
    int? Position = null,
    long? Value = null
);
