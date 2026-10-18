namespace Thunderfall.Application.Errors;

public sealed class GameApplicationException(string code, int status = 400) : Exception(code)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}
