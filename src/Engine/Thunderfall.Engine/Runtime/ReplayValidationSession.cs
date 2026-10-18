using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Abstractions.Events;
using Thunderfall.Engine.Evaluation;
using Thunderfall.Engine.Features;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Runtime;

internal sealed class ReplayValidationSession(CompiledDefinition definition, RoundOutcome outcome)
{
    private readonly Cell[] _cells = new Cell[outcome.InitialBoard.Cells.Length];
    private readonly HashSet<int> _seenIds = [];
    private readonly HashSet<int> _collected = [];
    private HashSet<int> _pendingRemoval = [];
    private long _ticks;
    private int _cascades;
    private int _sequence;
    private bool _generated;
    private bool _settled;
    private Board? _lastBefore;
    private SymbolsRemoved? _removalEvent;
    private SymbolsDropped? _dropEvent;
    private SymbolsSpawned? _spawnEvent;
    private readonly List<OrbCollection> _stepOrbs = [];
    public void Validate()
    {
        ValidateInitialState();
        foreach (var mathematicalEvent in outcome.MathematicalEvents)
        {
            Apply(mathematicalEvent);
        }

        ValidateFinalState();
    }

    private void ValidateInitialState()
    {
        Check(outcome.RulesetHash == definition.RulesetHash, "configuration hash");
        Check(outcome.InitialBoard.Layout == definition.Definition.Layout && outcome.FinalBoard.Layout == definition.Definition.Layout && outcome.InitialBoard.Cells.Length == definition.Definition.Layout.Size, "board geometry");
        Check(outcome.InitialBoard.Cells.All(c => definition.Definition.Symbols.Contains(c.Symbol) && (c.Symbol == SymbolId.StormOrb ? c.Multiplier > 0 : c.Multiplier == 0)), "initial cell contract");
    }

    private void ValidateFinalState()
    {
        Check(_generated && _settled && _cells.SequenceEqual(outcome.FinalBoard.Cells) && _cascades == outcome.CascadeSteps.Length, "final board/steps");
        Check(outcome.PayTicks == _ticks && outcome.RawWin == AmountMath.Raw(outcome.Stake, _ticks) && outcome.FinalWin == outcome.MathematicalEvents.OfType<RoundWinCalculated>().Single().Award, "outcome award");
        Check(outcome.MultiplierEvents.Length == _collected.Count && outcome.MultiplierEvents.Select(x => x.Identity).ToHashSet().SetEquals(_collected), "orb event list");
        if (definition.Definition.Kind == "CountAnywhere")
        {
            Check(ReferenceEvaluator.Evaluate(outcome.FinalBoard, definition.Definition).Length == 0, "terminal wins");
        }
    }

    private void Apply(MathEvent mathematicalEvent)
    {
        Check(mathematicalEvent.SequenceNumber == _sequence++ && mathematicalEvent.SchemaVersion == 1, "sequence/schema");
        switch (mathematicalEvent)
        {
            case BoardGenerated b:
                OnBoardGenerated(b);
                break;
            case WinningSymbolsDetected win:
                OnWinningSymbolsDetected(win);
                break;
            case MultiplierCollected m:
                OnMultiplierCollected(m);
                break;
            case SymbolsRemoved removed:
                OnSymbolsRemoved(removed);
                break;
            case SymbolsDropped dropped:
                OnSymbolsDropped(dropped);
                break;
            case SymbolsSpawned spawned:
                OnSymbolsSpawned(spawned);
                break;
            case CascadeCompleted complete:
                OnCascadeCompleted(complete);
                break;
            case BonusMultiplierUpdated b:
                OnBonusMultiplierUpdated(b);
                break;
            case ScatterTriggerDetected t:
                OnScatterTriggerDetected(t);
                break;
            case FreeSpinsStarted f:
                OnFreeSpinsStarted(f);
                break;
            case FreeSpinCompleted f:
                OnFreeSpinCompleted(f);
                break;
            case FreeSpinsRetriggered f:
                OnFreeSpinsRetriggered(f);
                break;
            case FeatureCompleted f:
                OnFeatureCompleted(f);
                break;
            case MaxWinReached cap:
                OnMaxWinReached(cap);
                break;
            case RoundWinCalculated win:
                OnRoundWinCalculated(win);
                break;
        }
    }

    private void OnBoardGenerated(BoardGenerated b)
    {
        Check(!_generated && b.Board.Cells.SequenceEqual(outcome.InitialBoard.Cells), "initial board");
        _generated = true;
        b.Board.Cells.CopyTo(_cells);
        foreach (var cell in _cells)
        {
            Check(cell.Identity > 0 && !cell.Collected && _seenIds.Add(cell.Identity), "initial identity");
        }
    }

    private void OnWinningSymbolsDetected(WinningSymbolsDetected win)
    {
        Check(_generated && !_settled, "event order");
        _lastBefore = Board.Snapshot(definition.Definition.Layout, _cells);
        _stepOrbs.Clear();
        if (definition.Definition.Kind == "CountAnywhere")
        {
            var expected = ReferenceEvaluator.Evaluate(_lastBefore, definition.Definition);
            Check(expected.Length == win.Groups.Length, "group count");
            for (var i = 0; i < expected.Length; i++)
            {
                Check(expected[i].Symbol == win.Groups[i].Symbol && expected[i].Count == win.Groups[i].Count && expected[i].PayTicks == win.Groups[i].PayTicks && expected[i].Positions.SequenceEqual(win.Groups[i].Positions), "reference award");
            }
        }
        else
        {
            var expected = new PaylineEvaluator().Evaluate(_cells, definition, true);
            Check(expected.Ticks == win.Groups.Sum(g => g.PayTicks), "payline award");
        }

        _ticks = checked(_ticks + win.Groups.Sum(g => g.PayTicks));
        _pendingRemoval = win.Groups.SelectMany(g => g.Positions).ToHashSet();
    }

    private void OnMultiplierCollected(MultiplierCollected m)
    {
        Check(_pendingRemoval.Count > 0 && m.Orb.Position >= 0 && m.Orb.Position < _cells.Length, "orb eligibility");
        var orb = _cells[m.Orb.Position];
        Check(orb.Symbol == SymbolId.StormOrb && orb.Identity == m.Orb.Identity && orb.Multiplier == m.Orb.Value && _collected.Add(orb.Identity) && !orb.Collected, "orb identity/once");
        _cells[m.Orb.Position] = orb with
        {
            Collected = true
        };
        _stepOrbs.Add(m.Orb);
    }

    private void OnSymbolsRemoved(SymbolsRemoved removed)
    {
        _removalEvent = removed;
        Check(_cells.Where(c => c.Symbol == SymbolId.StormOrb).All(c => c.Collected), "uncollected eligible orb");
        Check(_pendingRemoval.SetEquals(removed.Positions), "removed positions");
        foreach (var p in removed.Positions)
        {
            Check(_cells[p].Symbol >= SymbolId.Crown && _cells[p].Symbol <= SymbolId.YellowGem, "special removal");
            _cells[p] = default;
        }
    }

    private void OnSymbolsDropped(SymbolsDropped dropped)
    {
        _dropEvent = dropped;
        var previous = _cells.ToArray();
        foreach (var move in dropped.Moves)
        {
            Check(move.From >= 0 && move.To >= 0 && move.From < _cells.Length && move.To < _cells.Length && previous[move.From] == move.Cell && move.Cell.Symbol != SymbolId.Empty, "drop source");
            Check(move.From / definition.Definition.Layout.Rows == move.To / definition.Definition.Layout.Rows && move.To > move.From, "vertical drop");
            _cells[move.From] = default;
        }

        foreach (var move in dropped.Moves)
        {
            Check(_cells[move.To].Symbol == SymbolId.Empty, "drop destination");
            _cells[move.To] = move.Cell;
        }
    }

    private void OnSymbolsSpawned(SymbolsSpawned spawned)
    {
        _spawnEvent = spawned;
        foreach (var spawn in spawned.Symbols)
        {
            Check(spawn.Position >= 0 && spawn.Position < _cells.Length && _cells[spawn.Position].Symbol == SymbolId.Empty && spawn.Cell.Identity > 0 && _seenIds.Add(spawn.Cell.Identity), "spawn position/identity");
            Check(definition.Definition.Symbols.Contains(spawn.Cell.Symbol) && !spawn.Cell.Collected, "spawn symbol");
            _cells[spawn.Position] = spawn.Cell;
        }

        Check(_cells.All(c => c.Symbol != SymbolId.Empty), "unfilled cells");
    }

    private void OnCascadeCompleted(CascadeCompleted complete)
    {
        Check(complete.StepIndex == _cascades && complete.Board.Cells.SequenceEqual(_cells), "cascade snapshot");
        var step = outcome.CascadeSteps[_cascades++];
        Check(step.BoardBefore.Cells.SequenceEqual(_lastBefore!.Cells) && step.BoardAfter.Cells.SequenceEqual(_cells), "step snapshots");
        Check(step.RemovedPositions.SequenceEqual(_removalEvent!.Positions) && step.GravityMoves.SequenceEqual(_dropEvent!.Moves) && step.NewSymbols.SequenceEqual(_spawnEvent!.Symbols) && step.MultiplierEvents.SequenceEqual(_stepOrbs), "step movement/orb metadata");
        Check(step.BaseWin == AmountMath.Raw(outcome.Stake, step.WinningGroups.Sum(g => g.PayTicks)), "step amount");
        _pendingRemoval.Clear();
    }

    private void OnBonusMultiplierUpdated(BonusMultiplierUpdated b)
    {
        Check(outcome.StartingMathState.IsBonus && b.Before == outcome.StartingMathState.Bonus!.PersistentMultiplier && b.Collected == outcome.MultiplierEvents.Sum(o => (long)o.Value) && b.After == b.Before + b.Collected, "bonus multiplier event");
    }

    private void OnScatterTriggerDetected(ScatterTriggerDetected t)
    {
        Check(t.ScatterCount == outcome.InitialBoard.Cells.Count(c => c.Symbol == SymbolId.ThunderGate) && t.IsRetrigger == outcome.StartingMathState.IsBonus, "scatter event");
    }

    private void OnFreeSpinsStarted(FreeSpinsStarted f)
    {
        Check(!outcome.StartingMathState.IsBonus && f.State == outcome.FinalMathState.Bonus, "feature start event");
    }

    private void OnFreeSpinCompleted(FreeSpinCompleted f)
    {
        Check(outcome.StartingMathState.IsBonus && f.State == outcome.FinalMathState.Bonus, "free-spin event");
    }

    private void OnFreeSpinsRetriggered(FreeSpinsRetriggered f)
    {
        Check(f.AddedSpins == definition.Definition.FeatureRules.RetriggerSpins && f.State == outcome.FinalMathState.Bonus, "retrigger event");
    }

    private void OnFeatureCompleted(FeatureCompleted f)
    {
        Check(f.State == outcome.FinalMathState.Bonus && !f.State.Active, "feature complete event");
    }

    private void OnMaxWinReached(MaxWinReached cap)
    {
        Check(outcome.MaxWinReached && cap.Award == outcome.FinalWin && cap.NominalAward == outcome.NominalWin && cap.CycleAward == outcome.Stake * definition.Definition.MaxWinMultiplier, "max-win event");
    }

    private void OnRoundWinCalculated(RoundWinCalculated win)
    {
        var multiplier = outcome.StartingMathState.IsBonus ? checked(outcome.StartingMathState.Bonus!.PersistentMultiplier + outcome.MultiplierEvents.Sum(m => (long)m.Value)) : System.Math.Max(1, outcome.MultiplierEvents.Sum(m => (long)m.Value));
        var expectedSettlement = new SettlementRule().Evaluate(definition, new(outcome.Stake, outcome.RoundId), outcome.StartingMathState, outcome.InitialBoard.Cells.Count(c => c.Symbol == SymbolId.ThunderGate), outcome.MultiplierEvents.Sum(m => (long)m.Value), _ticks);
        Check(!_settled && win.PayTicks == _ticks && win.RawWin == AmountMath.Raw(outcome.Stake, _ticks) && win.EffectiveMultiplier == multiplier && win.NominalAward == AmountMath.Settle(outcome.Stake, _ticks, multiplier) && win.Award == expectedSettlement.Award, "settlement");
        Check(expectedSettlement.State == outcome.FinalMathState, "feature transition");
        _settled = true;
    }

    private static void Check(bool condition, string reason)
    {
        if (!condition)
        {
            throw new MathExecutionException("InvalidReplay: " + reason);
        }
    }
}
