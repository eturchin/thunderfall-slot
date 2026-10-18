using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Abstractions.BonusBuyPolicy;

/// <summary>Versioned entry into the existing Storm bonus. No base spin or forced symbols.</summary>
public static class BonusBuyPolicy
{
    public const string Version = "storm-buy-1.0.0";
    public const long PriceNumerator = 455;
    public const long PriceDenominator = 10;

    public static long Price(long stake) => checked(stake * PriceNumerator) / PriceDenominator;

    public static GameMathState Enter(CompiledDefinition compiled, long stake, Guid purchaseId)
    {
        var definition = compiled.Definition;
        if (definition.GameId != "thunderfall" || definition.MathVersion != "1.0.0")
        {
            throw new MathExecutionException("BonusBuyUnavailable");
        }

        if (!definition.Stakes.Contains(stake))
        {
            throw new MathExecutionException("InvalidStake");
        }

        return new(
            new(
                purchaseId,
                definition.GameId,
                definition.MathVersion,
                stake,
                definition.FeatureRules.InitialSpins,
                0,
                1,
                0,
                0,
                1,
                0
            )
        );
    }
}
