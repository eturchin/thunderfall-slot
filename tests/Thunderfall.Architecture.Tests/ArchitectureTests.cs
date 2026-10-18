using Thunderfall.Engine.Abstractions;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;
using Thunderfall.Engine.Runtime;

namespace Thunderfall.Architecture.Tests;

public sealed class ArchitectureTests
{
    [Theory]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Npgsql")]
    [InlineData("Thunderfall.Application")]
    [InlineData("Thunderfall.Infrastructure")]
    public void MathematicsDoesNotReferencePlatform(string forbidden)
    {
        foreach (
            var assembly in new[]
            {
                typeof(RoundEngine).Assembly,
                typeof(IGameMathModel).Assembly,
                typeof(AmountMath).Assembly,
                typeof(IRandomSource).Assembly,
            }
        )
        {
            Assert.DoesNotContain(
                assembly.GetReferencedAssemblies(),
                r => r.Name!.StartsWith(forbidden, StringComparison.Ordinal)
            );
        }
    }

    [Fact]
    public void PureEngineHasNoDefaultRandomState()
    {
        Assert.DoesNotContain(
            typeof(RoundEngine).GetFields(
                System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public
            ),
            f => typeof(IRandomSource).IsAssignableFrom(f.FieldType)
        );
    }
}
