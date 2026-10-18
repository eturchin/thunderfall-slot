using System.Collections.Immutable;
using Thunderfall.Engine.Abstractions.Events;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Abstractions.Contracts;

public sealed record RoundOutcome(
    Guid RoundId,
    string GameId,
    string MathVersion,
    string RulesetHash,
    string Mode,
    long Stake,
    Board InitialBoard,
    ImmutableArray<CascadeStep> CascadeSteps,
    ImmutableArray<WinningGroup> WinningGroups,
    ImmutableArray<OrbCollection> MultiplierEvents,
    decimal RawWin,
    long PayTicks,
    long EffectiveMultiplier,
    long NominalWin,
    long FinalWin,
    Board FinalBoard,
    GameMathState StartingMathState,
    GameMathState FinalMathState,
    ImmutableArray<MathEvent> MathematicalEvents,
    bool MaxWinReached,
    ImmutableArray<int> ReelStops
);
