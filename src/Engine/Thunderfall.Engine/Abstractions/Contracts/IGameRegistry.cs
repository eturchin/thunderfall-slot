namespace Thunderfall.Engine.Abstractions.Contracts;

public interface IGameRegistry
{
    IReadOnlyCollection<IGameMathModel> Games
    {
        get;
    }
    IGameMathModel Get(string gameId, string? version = null);
}
