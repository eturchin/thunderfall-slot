using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Abstractions.Contracts;

public interface IGameMathModel
{
    CompiledDefinition Configuration
    {
        get;
    }
    RoundOutcome Execute(
        RoundRequest request,
        GameMathState state,
        IRandomSource random,
        ExecutionOptions options = default
    );
    RoundSummary ExecuteSummary(
        RoundRequest request,
        GameMathState state,
        IRandomSource random,
        CancellationToken cancellationToken = default
    );
}
