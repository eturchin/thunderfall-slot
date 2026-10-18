namespace Thunderfall.Application.Entities;

public sealed class DemoWallet
{
    public Guid SessionId
    {
        get; set;
    }
    public long Balance
    {
        get; set;
    }
}
