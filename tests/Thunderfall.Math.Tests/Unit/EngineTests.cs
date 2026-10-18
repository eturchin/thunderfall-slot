using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Evaluation;
using Thunderfall.Engine.Features;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;
using Thunderfall.Engine.Runtime;
using Thunderfall.Tests;

namespace Thunderfall.Math.Tests.Unit;

public sealed class EngineTests
{
    [Fact]
    public void XoshiroGoldenVector()
    {
        var rng = new Xoshiro256StarStar(42);
        ulong[] expected =
        [
            0x15780B2E0C2EC716,
            0x6104D9866D113A7E,
            0xAE17533239E499A1,
            0xECB8AD4703B360A1,
            0xFDE6DC7FE2EC5E64,
        ];
        foreach (var x in expected)
        {
            Assert.Equal(x, rng.NextUInt64());
        }
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(3u)]
    [InlineData(16u)]
    [InlineData(uint.MaxValue)]
    public void BoundedDrawsStayInRange(uint bound)
    {
        var rng = new Xoshiro256StarStar(7);
        for (var i = 0; i < 10000; i++)
        {
            Assert.True(rng.NextBoundedUInt32(bound) < bound);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextBoundedUInt32(0));
    }

    [Theory]
    [InlineData(7, 0)]
    [InlineData(8, 5000000)]
    [InlineData(9, 5000000)]
    [InlineData(10, 15000000)]
    [InlineData(11, 15000000)]
    [InlineData(12, 50000000)]
    [InlineData(30, 50000000)]
    public void CountBandsSelectHighestApplicable(int n, long expected)
    {
        var d = new CompiledDefinition(Fixtures.InitialDefinition());
        var cells = Enumerable
            .Range(0, 30)
            .Select(i => new Cell(i < n ? SymbolId.Crown : SymbolId.ThunderGate))
            .ToArray();
        Span<int> counts = stackalloc int[12];
        Assert.Equal(expected, new CountAnywhereEvaluator().Evaluate(cells, d, counts).PayTicks);
    }

    [Fact]
    public void SpecialsNeverPay()
    {
        var m = Fixtures.Model();
        var cells = Enumerable.Repeat(new Cell(SymbolId.StormOrb, 2), 30).ToArray();
        Span<int> counts = stackalloc int[12];
        Assert.Equal(
            0,
            new CountAnywhereEvaluator().Evaluate(cells, m.Configuration, counts).PayTicks
        );
    }

    [Fact]
    public void SurvivingOrbCollectedOnlyOnceAcrossCascades()
    {
        var m = new RoundEngine(
            Fixtures.InitialDefinition(),
            new Fixtures.FixedGenerator(Fixtures.WinningBoard())
        );
        var r = m.Execute(new(1000, Guid.Empty), GameMathState.Empty, new Fixtures.RefillRandom(8));
        Assert.Equal(2, r.CascadeSteps.Length);
        Assert.Single(r.MultiplierEvents);
        Assert.Equal(2, r.EffectiveMultiplier);
        Assert.Equal(20000, r.FinalWin);
        ReplayValidator.Validate(m.Configuration, r);
    }

    [Fact]
    public void NoWinOrbsPayNothing()
    {
        var b = Fixtures.WinningBoard();
        for (var i = 0; i < 8; i++)
        {
            b[i] = new(SymbolId.ThunderGate, Identity: i + 1);
        }

        var m = new RoundEngine(Fixtures.Definition(), new Fixtures.FixedGenerator(b));
        var r = m.Execute(new(1000, Guid.Empty), GameMathState.Empty, new Fixtures.RefillRandom());
        Assert.Equal(0, r.FinalWin);
        Assert.Empty(r.MultiplierEvents);
        Assert.Equal(15, r.FinalMathState.Bonus!.RemainingSpins);
    }

    [Fact]
    public void CascadesAreNotSilentlyTruncated()
    {
        var d = Fixtures.Definition() with
        {
            ResourceLimits = new(1, 100000)
        };
        var m = new RoundEngine(d, new Fixtures.FixedGenerator(Fixtures.WinningBoard()));
        var e = Assert.Throws<MathExecutionException>(() =>
            m.Execute(new(1000, Guid.Empty), GameMathState.Empty, new Fixtures.RefillRandom(8))
        );
        Assert.Contains("WorkLimit", e.Message);
    }

    [Fact]
    public void BonusCarriesMultiplierAndRetriggers()
    {
        var d = new CompiledDefinition(Fixtures.Definition());
        var state = new GameMathState(
            new(
                Guid.Empty,
                d.Definition.GameId,
                d.Definition.MathVersion,
                1000,
                2,
                1,
                7,
                100,
                0,
                1,
                200
            )
        );
        var r = new SettlementRule().Evaluate(d, new(1000, Guid.Empty), state, 3, 5, 1000000);
        Assert.Equal(12, r.EffectiveMultiplier);
        Assert.Equal(12000, r.Award);
        Assert.Equal(6, r.State.Bonus!.RemainingSpins);
        Assert.Equal(1, r.State.Bonus.RetriggerCount);
        Assert.Equal(12200, r.State.Bonus.CycleAward);
    }

    [Fact]
    public void CapIncludesPreviousBaseAndBonusAwards()
    {
        var d = new CompiledDefinition(Fixtures.Definition());
        var cap = 1000 * d.Definition.MaxWinMultiplier;
        var state = new GameMathState(
            new(
                Guid.Empty,
                d.Definition.GameId,
                d.Definition.MathVersion,
                1000,
                5,
                1,
                10,
                0,
                0,
                1,
                cap - 17
            )
        );
        var r = new SettlementRule().Evaluate(d, new(1000, Guid.Empty), state, 4, 500, 50000000);
        Assert.Equal(17, r.Award);
        Assert.True(r.CapReached);
        Assert.Equal(0, r.State.Bonus!.RemainingSpins);
        Assert.Equal(9, r.AbandonedSpins);
        Assert.True(r.NominalAward > r.Award);
    }

    [Theory]
    [InlineData(1, 500000, 0)]
    [InlineData(3, 500000, 2)]
    [InlineData(5, 500000, 2)]
    public void SettlementRoundsOnceTiesEven(long stake, long ticks, long expected) =>
        Assert.Equal(expected, AmountMath.Settle(stake, ticks, 1));

    [Fact]
    public void CheckedAccountingRejectsOverflow() =>
        Assert.Throws<OverflowException>(() =>
            AmountMath.Settle(long.MaxValue, long.MaxValue, long.MaxValue)
        );

    [Fact]
    public void WeightedSamplerRejectsZeroAndSamplesIntegers()
    {
        Assert.Throws<ArgumentException>(() => new WeightedSampler([(1, 0u)]));
        var s = new WeightedSampler([(1, 0u), (2, 10u)]);
        var r = new Xoshiro256StarStar(9);
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(2, s.Sample(r));
        }
    }

    [Fact]
    public void InvalidConfigurationRejected()
    {
        var g = Fixtures.Definition() with
        {
            Stakes = [-1]
        };
        Assert.Throws<ArgumentException>(() => new CompiledDefinition(g));
    }

    [Fact]
    public void ReplayRejectsTamperedSettlement()
    {
        var m = Fixtures.Model();
        var r = m.Execute(new(1000, Guid.Empty), GameMathState.Empty, new Xoshiro256StarStar(12));
        Assert.Throws<MathExecutionException>(() =>
            ReplayValidator.Validate(m.Configuration, r with
            {
                FinalWin = r.FinalWin + 1
            })
        );
    }

    [Fact]
    public void ReplayRejectsUnsupportedSchemaAndGeometry()
    {
        var m = Fixtures.Model();
        var r = m.Execute(new(1000, Guid.Empty), GameMathState.Empty, new Xoshiro256StarStar(42));
        var events = r.MathematicalEvents.SetItem(
            0,
            r.MathematicalEvents[0] with
            {
                SchemaVersion = 99,
            }
        );
        Assert.Throws<MathExecutionException>(() =>
            ReplayValidator.Validate(m.Configuration, r with
            {
                MathematicalEvents = events
            })
        );
        Assert.Throws<MathExecutionException>(() =>
            ReplayValidator.Validate(
                m.Configuration,
                r with
                {
                    FinalBoard = r.FinalBoard with
                    {
                        Layout = new(5, 6)
                    },
                }
            )
        );
    }
}
