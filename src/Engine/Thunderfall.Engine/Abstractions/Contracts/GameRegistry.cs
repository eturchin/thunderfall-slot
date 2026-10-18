namespace Thunderfall.Engine.Abstractions.Contracts;

public sealed class GameRegistry : IGameRegistry
{
    private readonly IReadOnlyDictionary<(string, string), IGameMathModel> _models;

    public GameRegistry(IEnumerable<IGameMathModel> all) =>
        _models = all.ToDictionary(m =>
            (m.Configuration.Definition.GameId, m.Configuration.Definition.MathVersion)
        );

    public IReadOnlyCollection<IGameMathModel> Games => _models.Values.ToArray();

    public IGameMathModel Get(string gameId, string? version = null)
    {
        var candidates = _models.Values.Where(m => m.Configuration.Definition.GameId == gameId);
        var result = version is null
            ? candidates
                .OrderByDescending(m =>
                    Version.TryParse(m.Configuration.Definition.MathVersion, out var v)
                        ? v
                        : new Version(0, 0)
                )
                .ThenByDescending(
                    m => m.Configuration.Definition.MathVersion,
                    StringComparer.Ordinal
                )
                .FirstOrDefault()
            : candidates.FirstOrDefault(m => m.Configuration.Definition.MathVersion == version);
        return result ?? throw new KeyNotFoundException("InvalidGame");
    }
}
