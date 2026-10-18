using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Runtime;

namespace Thunderfall.Games.Classic;

public static class ClassicModule
{
    public static IGameMathModel Load(string path) =>
        new RoundEngine(DefinitionJson.Load(path), new ReelStripBoardGenerator());
}
