using System.Numerics;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Simulation.Analysis;

public static class ExactProbability
{
    public static BigInteger Choose(int n, int k)
    {
        if (k < 0 || k > n)
        {
            return 0;
        }

        BigInteger result = 1;
        for (var i = 1; i <= k; i++)
        {
            result = result * (n - i + 1) / i;
        }

        return result;
    }

    public static Fraction Count(int n, int k, uint weight, uint total) =>
        new(
            Choose(n, k) * BigInteger.Pow(weight, k) * BigInteger.Pow(total - weight, n - k),
            BigInteger.Pow(total, n)
        );

    public static Fraction AtLeast(int n, int minimum, uint weight, uint total)
    {
        BigInteger numerator = 0;
        for (var k = minimum; k <= n; k++)
        {
            numerator +=
                Choose(n, k) * BigInteger.Pow(weight, k) * BigInteger.Pow(total - weight, n - k);
        }

        return new(numerator, BigInteger.Pow(total, n));
    }

    public static Fraction AnyOrdinaryWin(
        int cells,
        int minimum,
        IReadOnlyList<uint> ordinaryWeights,
        uint total
    )
    {
        if (ordinaryWeights.Count > 12 || minimum < 1)
        {
            throw new ArgumentException("Enumeration budget");
        }

        BigInteger mass = 0;
        for (var mask = 1; mask < (1 << ordinaryWeights.Count); mask++)
        {
            var selected = BitOperations.PopCount((uint)mask);
            if (selected * minimum > cells)
            {
                continue;
            }

            var weights = ordinaryWeights.Where((_, i) => (mask & (1 << i)) != 0).ToArray();
            var rest = checked(total - (uint)weights.Sum(w => (long)w));
            BigInteger intersection = 0;
            void Visit(int index, int remaining, BigInteger coefficient)
            {
                if (index == weights.Length)
                {
                    intersection += coefficient * BigInteger.Pow(rest, remaining);
                    return;
                }
                var max = remaining - (weights.Length - index - 1) * minimum;
                for (var k = minimum; k <= max; k++)
                {
                    Visit(
                        index + 1,
                        remaining - k,
                        coefficient * Choose(remaining, k) * BigInteger.Pow(weights[index], k)
                    );
                }
            }
            Visit(0, cells, BigInteger.One);
            mass += selected % 2 == 1 ? intersection : -intersection;
        }
        return new(mass, BigInteger.Pow(total, cells));
    }

    public static object Analyze(GameDefinition definition, bool bonus = false)
    {
        var profile = bonus ? definition.BonusGenerationProfile : definition.BaseGenerationProfile;
        var total = checked((uint)profile.InitialWeights.Sum(w => (long)w.Weight));
        return new
        {
            definition.GameId,
            definition.MathVersion,
            definition.RulesetHash,
            Mode = bonus ? "Bonus" : "Base",
            Method = "Exact binomial marginal distributions for independent initial cells ONLY. Not exact cascade/cycle RTP.",
            AnyOrdinaryWin = AnyOrdinaryWin(
                    definition.Layout.Size,
                    8,
                    profile
                        .InitialWeights.Where(w =>
                            w.Symbol >= SymbolId.Crown && w.Symbol <= SymbolId.YellowGem
                        )
                        .Select(w => w.Weight)
                        .ToArray(),
                    total
                )
                .ToString(),
            Counts = profile
                .InitialWeights.Select(w => new
                {
                    w.Symbol,
                    Weight = w.Weight,
                    Distribution = Enumerable
                        .Range(0, definition.Layout.Size + 1)
                        .Select(k => new
                        {
                            Count = k,
                            Probability = Count(definition.Layout.Size, k, w.Weight, total).ToString(),
                            Approximation = Count(definition.Layout.Size, k, w.Weight, total).Approximation,
                        }),
                    EightOrMore = AtLeast(definition.Layout.Size, 8, w.Weight, total).ToString(),
                })
                .ToArray(),
            ScatterTrigger = AtLeast(
                    definition.Layout.Size,
                    bonus ? definition.FeatureRules.RetriggerScatters : definition.FeatureRules.TriggerScatters,
                    profile.InitialWeights.Single(w => w.Symbol == SymbolId.ThunderGate).Weight,
                    total
                )
                .ToString(),
            AnyOrb = AtLeast(
                    definition.Layout.Size,
                    1,
                    profile.InitialWeights.Single(w => w.Symbol == SymbolId.StormOrb).Weight,
                    total
                )
                .ToString(),
        };
    }

    // Tiny complete-round reference: enumerate all four two-cell boards on each refill.
    // At the design cap, all future histories have the same award and collapse into an absorbing state.
    public static IReadOnlyDictionary<int, Fraction> TinyTwoCellCycleDistribution(int maxAwards = 3)
    {
        var result = new SortedDictionary<int, Fraction>();
        for (var awards = 0; awards < maxAwards; awards++)
        {
            result[awards] = new(3, BigInteger.Pow(4, awards + 1));
        }

        result[maxAwards] = new(1, BigInteger.Pow(4, maxAwards));
        return result;
    }
}
