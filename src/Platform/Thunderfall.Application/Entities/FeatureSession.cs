namespace Thunderfall.Application.Entities;

public sealed class FeatureSession
{
    public Guid SessionId
    {
        get; set;
    }
    public string StateJson { get; set; } = "{}";
    public bool Active
    {
        get; set;
    }
    public string MathVersion { get; set; } = "";
    public long Version
    {
        get; set;
    }
    public DateTimeOffset UpdatedAt
    {
        get; set;
    }
}
