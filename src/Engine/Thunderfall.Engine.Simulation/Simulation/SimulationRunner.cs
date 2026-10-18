using System.Diagnostics;
using System.Runtime.InteropServices;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Simulation.Simulation;

public sealed class SimulationRunner
{
    public SimulationReport Run(
        IGameMathModel model,
        SimulationOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.PaidCycles);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Workers);
        cancellationToken.ThrowIfCancellationRequested();
        if (options.PaidCycles > 1_000_000_000 || options.Workers > 256)
        {
            throw new ArgumentOutOfRangeException(nameof(options));
        }

        var definition = model.Configuration.Definition;
        var stake = options.Stake ?? definition.Stakes[0];
        if (!definition.Stakes.Contains(stake))
        {
            throw new ArgumentException("InvalidStake");
        }

        var domain = Xoshiro256StarStar.Domain(definition.GameId, definition.MathVersion);
        var chunks =
            (options.PaidCycles + Xoshiro256StarStar.ChunkSize - 1) / Xoshiro256StarStar.ChunkSize;
        var process = Process.GetCurrentProcess();
        var cpuStart = process.TotalProcessorTime;
        var bytes = GC.GetTotalAllocatedBytes(true);
        var watch = Stopwatch.StartNew();
        var total = new ChunkStats();
        var window = System.Math.Min(128, options.Workers * 4);
        // Bounded batches keep memory independent of the total cycle count. Merge chunks by absolute id.
        for (long batch = 0; batch < chunks; batch += window)
        {
            var length = (int)System.Math.Min(window, chunks - batch);
            var results = new ChunkStats[length];
            Parallel.For(
                0,
                length,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = options.Workers,
                    CancellationToken = cancellationToken,
                },
                i =>
                {
                    var first = (batch + i) * Xoshiro256StarStar.ChunkSize;
                    var last = System.Math.Min(
                            options.PaidCycles,
                            first + Xoshiro256StarStar.ChunkSize
                        );
                    var s = new ChunkStats();
                    var rng = new Xoshiro256StarStar(0);
                    for (var index = first; index < last; index++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        rng.Reset(
                            Xoshiro256StarStar.CycleSeed(
                                options.Seed,
                                options.StreamId,
                                index,
                                domain
                            )
                        );
                        var request = new RoundRequest(stake, Guid.Empty);
                        var result = model.ExecuteSummary(request, GameMathState.Empty, rng, cancellationToken);
                        var baseAward = result.Award;
                        long bonusAward = 0;
                        var triggered = result.Triggered;
                        var capped = result.CapReached;
                        s.RecordRound(result, false);
                        var state = result.State;
                        while (state.IsBonus)
                        {
                            result = model.ExecuteSummary(request, state, rng, cancellationToken);
                            state = result.State;
                            s.RecordRound(result, true);
                            bonusAward = checked(bonusAward + result.Award);
                            capped |= result.CapReached;
                        }
                        s.RecordCycle(stake, baseAward, bonusAward, triggered, capped);
                    }
                    results[i] = s;
                }
            );
            foreach (var stats in results)
            {
                total.Merge(stats);
            }
        }
        var seconds = watch.Elapsed.TotalSeconds;
        var cpu = (process.TotalProcessorTime - cpuStart).TotalSeconds;
        var allocated = GC.GetTotalAllocatedBytes(true) - bytes;
        var sd = System.Math.Sqrt(total.Moments.Variance);
        var se = sd / System.Math.Sqrt(options.PaidCycles);
        var wager = checked(stake * options.PaidCycles);
        var award = checked(total.BaseAward + total.BonusAward);
        int[] values = [2, 3, 5, 8, 10, 15, 20, 25, 50, 100, 250, 500];
        var orbDistribution = new SortedDictionary<int, long>();
        for (var i = 0; i < values.Length; i++)
        {
            orbDistribution[values[i]] = total.Orbs[i];
        }

        return new(
            definition.GameId,
            definition.MathVersion,
            model.Configuration.RulesetHash,
            options.PaidCycles,
            options.PaidCycles,
            total.BonusSpins,
            wager,
            total.BaseAward,
            total.BonusAward,
            award,
            (double)award / wager,
            (double)total.BaseAward / wager,
            (double)total.BonusAward / wager,
            (double)total.Hits / options.PaidCycles,
            (double)total.Triggers / options.PaidCycles,
            total.BonusSpins == 0 ? 0 : (double)total.Retriggers / total.BonusSpins,
            total.Cascades,
            total.MaxDepth,
            (double)total.MultiplierRounds / (options.PaidCycles + total.BonusSpins),
            orbDistribution,
            total.BonusDistribution,
            total.Maximum,
            total.Caps,
            new(
                sd,
                total.Moments.Variance,
                1d - (double)total.Hits / options.PaidCycles,
                (double)total.HundredX / options.PaidCycles,
                (double)total.Caps / options.PaidCycles
            ),
            se,
            new(total.Moments.Mean - 1.96 * se, total.Moments.Mean + 1.96 * se),
            options.Seed,
            options.StreamId,
            Xoshiro256StarStar.Version,
            Xoshiro256StarStar.ChunkSize,
            options.Workers,
            seconds,
            options.PaidCycles / seconds,
            allocated,
            cpu,
            cpu / seconds / Environment.ProcessorCount,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            Environment.ProcessorCount
        );
    }

    private sealed class ChunkStats
    {
        public long BonusSpins;
        public long BaseAward;
        public long BonusAward;
        public long Hits;
        public long Triggers;
        public long Retriggers;
        public long Caps;
        public long Maximum;
        public long HundredX;
        public long MultiplierRounds;
        public int MaxDepth;
        public readonly RunningMoments Moments = new();
        public readonly SortedDictionary<int, long> Cascades = new();
        public readonly SortedDictionary<string, long> BonusDistribution = new();
        public readonly long[] Orbs = new long[12];

        public void RecordRound(RoundSummary r, bool bonus)
        {
            if (bonus)
            {
                BonusSpins++;
            }

            if (r.Retriggered)
            {
                Retriggers++;
            }

            Cascades[r.Cascades] = Cascades.GetValueOrDefault(r.Cascades) + 1;
            MaxDepth = System.Math.Max(MaxDepth, r.Cascades);
            if (r.CollectedMultiplier > 0)
            {
                MultiplierRounds++;
            }

            for (var i = 0; i < 12; i++)
            {
                Orbs[i] += r.Orbs.Get(i);
            }
        }

        public void RecordCycle(
            long stake,
            long baseAward,
            long bonusAward,
            bool triggered,
            bool capped
        )
        {
            BaseAward = checked(BaseAward + baseAward);
            BonusAward = checked(BonusAward + bonusAward);
            var win = checked(baseAward + bonusAward);
            if (win > 0)
            {
                Hits++;
            }

            if (capped)
            {
                Caps++;
            }

            if (win >= stake * 100)
            {
                HundredX++;
            }

            Maximum = System.Math.Max(Maximum, win);
            Moments.Add((double)win / stake);
            if (triggered)
            {
                Triggers++;
                var bucket =
                    bonusAward == 0 ? "0"
                    : bonusAward < stake ? "0-1x"
                    : bonusAward < stake * 10 ? "1-10x"
                    : bonusAward < stake * 100 ? "10-100x"
                    : bonusAward < stake * 1000 ? "100-1000x"
                    : "1000x+";
                BonusDistribution[bucket] = BonusDistribution.GetValueOrDefault(bucket) + 1;
            }
        }

        public void Merge(ChunkStats s)
        {
            BonusSpins = checked(BonusSpins + s.BonusSpins);
            BaseAward = checked(BaseAward + s.BaseAward);
            BonusAward = checked(BonusAward + s.BonusAward);
            Hits += s.Hits;
            Triggers += s.Triggers;
            Retriggers += s.Retriggers;
            Caps += s.Caps;
            Maximum = System.Math.Max(Maximum, s.Maximum);
            HundredX += s.HundredX;
            MultiplierRounds += s.MultiplierRounds;
            MaxDepth = System.Math.Max(MaxDepth, s.MaxDepth);
            Moments.Merge(s.Moments);
            foreach (var (k, v) in s.Cascades)
            {
                Cascades[k] = Cascades.GetValueOrDefault(k) + v;
            }

            foreach (var (k, v) in s.BonusDistribution)
            {
                BonusDistribution[k] = BonusDistribution.GetValueOrDefault(k) + v;
            }

            for (var i = 0; i < 12; i++)
            {
                Orbs[i] += s.Orbs[i];
            }
        }
    }
}
