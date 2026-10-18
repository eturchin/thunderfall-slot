using Thunderfall.Engine.Abstractions.Definitions;

namespace Thunderfall.Engine.Abstractions.Contracts;

public interface IFeatureRule
{
    FeatureTransition Evaluate(
        CompiledDefinition definition,
        RoundRequest request,
        GameMathState state,
        int initialScatters,
        long collectedMultiplier,
        long payTicks
    );
}
