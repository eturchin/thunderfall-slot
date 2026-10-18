using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Runtime;

namespace Thunderfall.Games.Storm;

public static class StormModule
{
    public static IGameMathModel Load(string path) => new RoundEngine(DefinitionJson.Load(path));
}
