using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;

namespace Thunderfall.Engine.Runtime;

public static class ReplayValidator
{
    public static void Validate(CompiledDefinition definition, RoundOutcome outcome) => new ReplayValidationSession(definition, outcome).Validate();
}
