using System.Collections.Immutable;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;
using Thunderfall.Engine.Runtime;
using Thunderfall.Engine.Simulation.Analysis;
using Thunderfall.Engine.Simulation.Simulation;
using Thunderfall.Tests;

namespace Thunderfall.Math.Tests.Simulation;

public sealed class SimulationTests
{
    [Fact]
    public void FrozenModelGoldenTotals()
    {
        var report = new SimulationRunner().Run(Fixtures.Model(), new(10000, 42, Workers: 2));
        Assert.Equal(10110708, report.TotalAward);
        Assert.Equal(7133093, report.TotalBaseWin);
        Assert.Equal(2977615, report.TotalBonusWin);
    }

    [Fact]
    public void WorkerCountDoesNotChangeResults()
    {
        var model = Fixtures.Model();
        var runner = new SimulationRunner();
        var a = runner.Run(model, new(20000, 42, Workers: 1));
        var b = runner.Run(model, new(20000, 42, Workers: 4));
        Assert.Equal(a.TotalAward, b.TotalAward);
        Assert.Equal(a.BonusSpins, b.BonusSpins);
        Assert.Equal(a.CascadeCountDistribution, b.CascadeCountDistribution);
        Assert.Equal(a.MultiplierValueDistribution, b.MultiplierValueDistribution);
        Assert.Equal(a.ConfidenceInterval95, b.ConfidenceInterval95);
        Assert.Equal(a.TotalBaseWin + a.TotalBonusWin, a.TotalAward);
        Assert.Equal(a.BaseRtpContribution + a.BonusRtpContribution, a.ObservedRtp, 12);
    }

    [Fact]
    public void CycleStreamsAreStableAndDomainSeparated()
    {
        var d = Xoshiro256StarStar.Domain("thunderfall", "1.0.0");
        var seen = new HashSet<ulong>();
        for (var i = 0; i < 20000; i++)
        {
            Assert.True(seen.Add(Xoshiro256StarStar.CycleSeed(42, 0, i, d)));
        }

        Assert.NotEqual(
            Xoshiro256StarStar.CycleSeed(42, 0, 0, d),
            Xoshiro256StarStar.CycleSeed(42, 1, 0, d)
        );
    }

    [Fact]
    public void BoundedSamplingPassesFixedChiSquareCheck()
    {
        var rng = new Xoshiro256StarStar(42);
        var counts = new int[17];
        for (var i = 0; i < 170000; i++)
        {
            counts[rng.NextBoundedUInt32(17)]++;
        }

        var chi = counts.Sum(x => (x - 10000d) * (x - 10000d) / 10000);
        Assert.InRange(chi, 1, 45);
    }

    [Fact]
    public void MultinomialUnionCountsOverlappingEventsExactly()
    {
        Assert.Equal(
            new Fraction(1, 2),
            ExactProbability.AnyOrdinaryWin(2, 2, new uint[] { 1, 1 }, 2)
        );
        Assert.Equal(
            new Fraction(1, 1),
            ExactProbability.AnyOrdinaryWin(2, 1, new uint[] { 1, 1 }, 2)
        );
    }

    [Fact]
    public void ExactBinomialMassSumsToOne()
    {
        Fraction total = new(0, 1);
        for (var k = 0; k <= 30; k++)
        {
            total += ExactProbability.Count(30, k, 25, 1000);
        }

        Assert.Equal(new Fraction(1, 1), total);
        Assert.Equal(new Fraction(1, 4), ExactProbability.Count(2, 2, 1, 2));
    }

    [Fact]
    public void InitialScatterFrequencyMatchesExactProbability()
    {
        var m = Fixtures.Model();
        var g = new WeightedCellBoardGenerator();
        var rng = new Xoshiro256StarStar(234);
        var cells = new Cell[30];
        var identity = 0;
        var triggers = 0;
        const int n = 100000;
        for (var i = 0; i < n; i++)
        {
            g.Generate(cells, m.Configuration, m.Configuration.Base, rng, ref identity);
            if (cells.Count(c => c.Symbol == SymbolId.ThunderGate) >= 4)
            {
                triggers++;
            }
        }
        var expected = ExactProbability.AtLeast(30, 4, 25, 1000).Approximation;
        Assert.InRange((double)triggers / n, expected - .001, expected + .001);
    }

    [Fact]
    public void TinyCompleteCycleMatchesExhaustiveAbsorbingEnumeration()
    {
        var d = Fixtures.Definition();
        var profile = new GenerationProfile(
            [new(SymbolId.Crown, 1), new(SymbolId.ThunderGate, 1)],
            [new(SymbolId.Crown, 1), new(SymbolId.ThunderGate, 1)],
            [new(2, 1)]
        );
        d = d with
        {
            Layout = new(1, 2),
            Symbols = [SymbolId.Crown, SymbolId.ThunderGate],
            Paytable = ImmutableDictionary<SymbolId, ImmutableArray<PayBand>>.Empty.Add(
                SymbolId.Crown,
                [new(2, 1000000)]
            ),
            BaseGenerationProfile = profile,
            BonusGenerationProfile = profile,
            MaxWinMultiplier = 3,
        };
        var model = new RoundEngine(d);
        var distribution = ExactProbability.TinyTwoCellCycleDistribution();
        var exact = distribution.Sum(x => x.Key * x.Value.Approximation);
        Assert.Equal(21d / 64, exact);
        var report = new SimulationRunner().Run(model, new(200000, 42, Workers: 2));
        Assert.InRange(report.ObservedRtp, exact - .005, exact + .005);
    }

    [Fact]
    public void WelfordChanPreservesKnownMoments()
    {
        var a = new RunningMoments();
        var b = new RunningMoments();
        foreach (var v in new[] { 1d, 2, 3 })
        {
            a.Add(v);
        }

        foreach (var v in new[] { 4d, 5 })
        {
            b.Add(v);
        }

        a.Merge(b);
        Assert.Equal(3, a.Mean, 12);
        Assert.Equal(2.5, a.Variance, 12);
    }
}
