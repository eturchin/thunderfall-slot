using System.Collections.Immutable;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Application.Contracts;

public sealed record RoundResponse(
    Guid RoundId,
    string GameId,
    string MathVersion,
    string Mode,
    long Stake,
    Board InitialBoard,
    ImmutableArray<CascadeStep> CascadeSteps,
    ImmutableArray<WinningGroup> WinningGroups,
    ImmutableArray<OrbCollection> MultiplierEvents,
    decimal RawWin,
    long FinalWin,
    long BalanceAfter,
    GameMathState FeatureState,
    ImmutableArray<PresentationHint> PresentationHints,
    RoundOutcome Outcome,
    BonusPurchase? BonusPurchase = null,
    long BalanceBefore = 0,
    long StakeDebited = 0,
    long BalanceAfterDebit = 0,
    long TotalWin = 0
);
