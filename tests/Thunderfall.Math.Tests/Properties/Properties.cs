using FsCheck.Xunit;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Evaluation;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;
using Thunderfall.Engine.Runtime;
using Thunderfall.Tests;

namespace Thunderfall.Math.Tests.Properties;

public sealed class Properties
{
    [Property(MaxTest = 250)]
    public bool GravityPreservesSurvivorOrderAndFillsExactly(int seed)
    {
        var d = new CompiledDefinition(Fixtures.Definition());
        var rng = new Xoshiro256StarStar(unchecked((ulong)seed));
        var cells = Fixtures.WinningBoard();
        var before = cells.ToArray();
        var mask = rng.NextUInt64() & ((1UL << 30) - 1);
        var identity = 30;
        var drops = new List<SymbolDrop>();
        var spawns = new List<SymbolSpawn>();
        new GravityCascadePolicy().Apply(cells, mask, d, d.Base, rng, ref identity, drops, spawns);
        for (var c = 0; c < 6; c++)
        {
            var expected = Enumerable
                .Range(c * 5, 5)
                .Where(p => (mask & (1UL << p)) == 0)
                .Select(p => before[p].Identity)
                .ToArray();
            var actual = cells
                .Skip(c * 5)
                .Take(5)
                .Where(x => x.Identity <= 30)
                .Select(x => x.Identity)
                .ToArray();
            if (!expected.SequenceEqual(actual))
            {
                return false;
            }
        }
        return cells.All(x => x.Symbol != SymbolId.Empty)
            && cells.Select(x => x.Identity).Distinct().Count() == 30
            && spawns.Count == System.Numerics.BitOperations.PopCount(mask)
            && Enumerable
                .Range(0, 30)
                .Where(i => (mask & (1UL << i)) != 0)
                .All(i => cells.All(c => c.Identity != before[i].Identity));
    }

    [Property(MaxTest = 200)]
    public bool OptimizedEvaluatorMatchesIndependentReference(int seed)
    {
        var m = Fixtures.Model();
        var rng = new Xoshiro256StarStar(unchecked((ulong)seed));
        Span<Cell> cells = stackalloc Cell[30];
        var identity = 0;
        new WeightedCellBoardGenerator().Generate(
            cells,
            m.Configuration,
            m.Configuration.Base,
            rng,
            ref identity
        );
        Span<int> counts = stackalloc int[12];
        var actual = new CountAnywhereEvaluator().Evaluate(cells, m.Configuration, counts);
        var reference = ReferenceEvaluator.Evaluate(
            Board.Snapshot(m.Configuration.Definition.Layout, cells),
            m.Configuration.Definition
        );
        return actual.PayTicks == reference.Sum(x => x.PayTicks);
    }

    [Property(MaxTest = 200)]
    public bool TracedAndFastExecutionAreIdenticalAndReplayable(int seed)
    {
        var m = Fixtures.Model();
        var value = unchecked((ulong)seed);
        var request = new RoundRequest(1000, Guid.Empty);
        var full = m.Execute(request, GameMathState.Empty, new Xoshiro256StarStar(value));
        var fast = m.ExecuteSummary(request, GameMathState.Empty, new Xoshiro256StarStar(value));
        ReplayValidator.Validate(m.Configuration, full);
        return full.FinalWin == fast.Award
            && full.PayTicks == fast.PayTicks
            && full.EffectiveMultiplier == fast.EffectiveMultiplier
            && full.FinalMathState == fast.State
            && full.FinalWin >= 0
            && full.FinalWin <= 5000000;
    }
}
