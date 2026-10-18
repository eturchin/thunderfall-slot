namespace Thunderfall.Application.Entities;

public sealed class GameSession
{
    public Guid Id
    {
        get; set;
    }
    public string GameId { get; set; } = "";
    public string MathVersion { get; set; } = "";
    public string RulesetHash { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public long Version
    {
        get; set;
    }
    public DateTimeOffset CreatedAt
    {
        get; set;
    }
    public DateTimeOffset UpdatedAt
    {
        get; set;
    }
}
