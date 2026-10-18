using System.Collections.Immutable;
using System.Numerics;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Abstractions.Events;
using Thunderfall.Engine.Evaluation;
using Thunderfall.Engine.Features;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Runtime;

public sealed class RoundEngine : IGameMathModel
{
    private readonly IBoardGenerator _generator;
    private readonly IWinEvaluator _evaluator;
    private readonly ICascadePolicy _cascade;
    private readonly IFeatureRule _settlement;
    public CompiledDefinition Configuration
    {
        get;
    }

    public RoundEngine(
        GameDefinition definition,
        IBoardGenerator? generator = null,
        IWinEvaluator? evaluator = null,
        ICascadePolicy? cascade = null,
        IFeatureRule? settlement = null
    )
    {
        Configuration = new(definition);
        this._generator = generator ?? new WeightedCellBoardGenerator();
        this._evaluator = evaluator ?? new CountAnywhereEvaluator();
        this._cascade = cascade ?? new GravityCascadePolicy();
        this._settlement = settlement ?? new SettlementRule();
    }

    public RoundOutcome Execute(
        RoundRequest request,
        GameMathState state,
        IRandomSource random,
        ExecutionOptions options = default
    ) => Run(request, state, random, true, options.CancellationToken).Outcome!;

    public RoundSummary ExecuteSummary(
        RoundRequest request,
        GameMathState state,
        IRandomSource random,
        CancellationToken cancellationToken = default
    ) => Run(request, state, random, false, cancellationToken).Summary;

    private (RoundSummary Summary, RoundOutcome? Outcome) Run(
        RoundRequest request,
        GameMathState state,
        IRandomSource rng,
        bool trace,
        CancellationToken cancellationToken
    )
    {
        var definition = Configuration.Definition;
        Validate(request, state, definition);
        cancellationToken.ThrowIfCancellationRequested();
        Span<Cell> board = stackalloc Cell[definition.Layout.Size];
        Span<int> counts = stackalloc int[12];
        Span<long> orbCounts = stackalloc long[12];
        orbCounts.Clear();
        var profile = state.IsBonus ? Configuration.Bonus : Configuration.Base;
        var identity = 0;
        Span<int> stops = stackalloc int[definition.Layout.Columns];
        stops.Fill(-1);
        if (definition.Kind == "Paylines")
        {
            ReelStripBoardGenerator.GenerateWithStops(
                board,
                Configuration,
                rng,
                ref identity,
                stops
            );
        }
        else
        {
            _generator.Generate(board, Configuration, profile, rng, ref identity);
        }

        var scatters = 0;
        foreach (var cell in board)
        {
            if (cell.Symbol == SymbolId.ThunderGate)
            {
                scatters++;
            }
        }

        var initial = trace ? Board.Snapshot(definition.Layout, board) : null;
        var events = trace ? new List<MathEvent>() : null;
        var steps = trace ? ImmutableArray.CreateBuilder<CascadeStep>() : null;
        var allGroups = trace ? ImmutableArray.CreateBuilder<WinningGroup>() : null;
        var allOrbs = trace ? ImmutableArray.CreateBuilder<OrbCollection>() : null;
        var sequence = 0;
        if (trace)
        {
            events!.Add(new BoardGenerated(sequence++, initial!));
        }

        long ticks = 0;
        long collected = 0;
        var depth = 0;
        if (definition.Kind == "Paylines")
        {
            var result = new PaylineEvaluator().Evaluate(board, Configuration, trace);
            ticks = result.Ticks;
            if (trace && result.Groups.Length > 0)
            {
                allGroups!.AddRange(result.Groups);
                events!.Add(new WinningSymbolsDetected(sequence++, result.Groups));
            }
        }
        else
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var wins = _evaluator.Evaluate(board, Configuration, counts);
                if (wins.PayTicks == 0)
                {
                    break;
                }

                if (depth >= definition.ResourceLimits.MaxCascades)
                {
                    throw new MathExecutionException("CascadeWorkLimitExceeded");
                }

                ticks = checked(ticks + wins.PayTicks);
                var before = trace ? Board.Snapshot(definition.Layout, board) : null;
                var groups = trace ? _evaluator.Describe(board, Configuration) : [];
                if (trace)
                {
                    allGroups!.AddRange(groups);
                    events!.Add(new WinningSymbolsDetected(sequence++, groups));
                }
                var orbs = trace ? ImmutableArray.CreateBuilder<OrbCollection>() : null;
                for (var i = 0; i < board.Length; i++)
                {
                    if (board[i].Symbol == SymbolId.StormOrb && !board[i].Collected)
                    {
                        var cell = board[i];
                        collected = checked(collected + cell.Multiplier);
                        board[i] = cell with
                        {
                            Collected = true
                        };
                        orbCounts[OrbIndex(cell.Multiplier)]++;
                        if (trace)
                        {
                            var orb = new OrbCollection(i, cell.Identity, cell.Multiplier);
                            orbs!.Add(orb);
                            allOrbs!.Add(orb);
                            events!.Add(new MultiplierCollected(sequence++, orb));
                        }
                    }
                }

                var removed = trace ? Positions(wins.RemovedPositions) : [];
                var drops = trace ? new List<SymbolDrop>() : null;
                var spawns = trace ? new List<SymbolSpawn>() : null;
                if (trace)
                {
                    events!.Add(new SymbolsRemoved(sequence++, removed));
                }

                _cascade.Apply(
                    board,
                    wins.RemovedPositions,
                    Configuration,
                    profile,
                    rng,
                    ref identity,
                    drops,
                    spawns
                );
                if (trace)
                {
                    var after = Board.Snapshot(definition.Layout, board);
                    var moved = drops!.ToImmutableArray();
                    var added = spawns!.ToImmutableArray();
                    events!.Add(new SymbolsDropped(sequence++, moved));
                    events.Add(new SymbolsSpawned(sequence++, added));
                    events.Add(new CascadeCompleted(sequence++, depth, after));
                    steps!.Add(
                        new(
                            depth,
                            before!,
                            groups,
                            removed,
                            moved,
                            added,
                            after,
                            AmountMath.Raw(request.Stake, wins.PayTicks),
                            orbs!.ToImmutable()
                        )
                    );
                }
                depth++;
            }
        }

        var transition = _settlement.Evaluate(
            Configuration,
            request,
            state,
            scatters,
            collected,
            ticks
        );
        if (trace)
        {
            SettlementEventWriter.Append(events!, ref sequence, definition, request, state, transition, ticks, collected, scatters);
        }
        var histogram = new OrbHistogram(
                    orbCounts[0],
                    orbCounts[1],
                    orbCounts[2],
                    orbCounts[3],
                    orbCounts[4],
                    orbCounts[5],
                    orbCounts[6],
                    orbCounts[7],
                    orbCounts[8],
                    orbCounts[9],
                    orbCounts[10],
                    orbCounts[11]
                );
        var summary = new RoundSummary(
            transition.Award,
            ticks,
            collected,
            depth,
            scatters,
            transition.State,
            transition.CapReached,
            transition.Triggered,
            transition.Retriggered,
            transition.EffectiveMultiplier,
            histogram
        );
        if (!trace)
        {
            return (summary, null);
        }

        return (
            summary,
            new(
                request.RoundId,
                definition.GameId,
                definition.MathVersion,
                Configuration.RulesetHash,
                state.IsBonus ? "FreeSpin" : "Base",
                request.Stake,
                initial!,
                steps!.ToImmutable(),
                allGroups!.ToImmutable(),
                allOrbs!.ToImmutable(),
                AmountMath.Raw(request.Stake, ticks),
                ticks,
                transition.EffectiveMultiplier,
                transition.NominalAward,
                transition.Award,
                Board.Snapshot(definition.Layout, board),
                state,
                transition.State,
                events!.ToImmutableArray(),
                transition.CapReached,
                stops.ToArray().ToImmutableArray()
            )
        );
    }

    private static ImmutableArray<int> Positions(ulong mask)
    {
        var a = ImmutableArray.CreateBuilder<int>();
        while (mask != 0)
        {
            var p = BitOperations.TrailingZeroCount(mask);
            a.Add(p);
            mask &= mask - 1;
        }
        return a.ToImmutable();
    }

    private static int OrbIndex(int value) =>
        value switch
        {
            2 => 0,
            3 => 1,
            5 => 2,
            8 => 3,
            10 => 4,
            15 => 5,
            20 => 6,
            25 => 7,
            50 => 8,
            100 => 9,
            250 => 10,
            500 => 11,
            _ => throw new MathExecutionException("InvalidOrbValue"),
        };

    private static void Validate(RoundRequest request, GameMathState state, GameDefinition definition)
    {
        if (!definition.Stakes.Contains(request.Stake))
        {
            throw new MathExecutionException("InvalidStake");
        }

        if (state.IsBonus)
        {
            var b = state.Bonus!;
            if (
                b.GameId != definition.GameId
                || b.MathVersion != definition.MathVersion
                || b.OriginalStake != request.Stake
                || b.PersistentMultiplier < 1
                || b.CycleAward < 0
                || b.CycleAward >= checked(request.Stake * definition.MaxWinMultiplier)
                || b.PlayedSpins < 0
                || b.RetriggerCount < 0
                || b.AccumulatedWin < 0
                || b.AccumulatedWin > b.CycleAward
                || b.StateVersion != 1
            )
            {
                throw new MathExecutionException("InvalidBonusState");
            }

            if (b.PlayedSpins >= definition.ResourceLimits.MaxBonusRounds)
            {
                throw new MathExecutionException("BonusWorkLimitExceeded");
            }
        }
    }
}
