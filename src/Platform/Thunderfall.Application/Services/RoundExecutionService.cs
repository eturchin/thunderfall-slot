using System.Globalization;
using Thunderfall.Application.Contracts;
using Thunderfall.Application.Entities;
using Thunderfall.Application.Errors;
using Thunderfall.Application.Persistence;
using Thunderfall.Application.Presentation;
using Thunderfall.Application.Security;
using Thunderfall.Engine.Abstractions.BonusBuyPolicy;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Application.Services;

public sealed class RoundExecutionService(
    ISessionStore store,
    IGameRegistry registry,
    IRandomSource random)
{
    public Task<RoundResponse> ExecuteAsync(
        Guid sessionId,
        string? secret,
        RoundCommand command,
        CancellationToken cancellationToken)
    {
        if (command.IdempotencyKey == Guid.Empty)
        {
            throw new GameApplicationException("InvalidIdempotencyKey");
        }

        return store.ExecuteAsync(
            sessionId,
            transaction => ExecuteLockedAsync(transaction, secret, command, cancellationToken),
            cancellationToken);
    }

    private async Task<RoundResponse> ExecuteLockedAsync(
        ISessionTransaction transaction,
        string? secret,
        RoundCommand command,
        CancellationToken cancellationToken)
    {
        SessionToken.Authorize(transaction.Session, secret);
        var fingerprint = CreateFingerprint(command);
        var previous = await transaction.FindOperationAsync(command.IdempotencyKey, cancellationToken);
        if (previous.HasValue)
        {
            if (previous.Value.Idempotency.Fingerprint != fingerprint)
            {
                throw new GameApplicationException("IdempotencyConflict", 409);
            }

            return RoundResponseSerializer.Deserialize(previous.Value.Round.OutcomeJson);
        }

        var model = registry.ResolveModel(transaction.Session.GameId, transaction.Session.MathVersion);
        var stake = ResolveStake(transaction, command, model);
        var debit = transaction.State.IsBonus ? 0 : command.BuyBonus ? BonusBuyPolicy.Price(stake) : stake;
        if (transaction.Balance < debit)
        {
            throw new GameApplicationException("InsufficientBalance", 409);
        }

        var roundId = Guid.NewGuid();
        var startingState = command.BuyBonus
            ? BonusBuyPolicy.Enter(model.Configuration, stake, roundId)
            : transaction.State;
        var outcome = ExecuteMath(model, new RoundRequest(stake, roundId), startingState, cancellationToken);
        var timestamp = DateTimeOffset.UtcNow;
        ApplyAccounting(transaction, command, outcome, debit, timestamp);
        var response = CreateResponse(transaction, command, outcome, debit, model.Configuration.Definition);
        RoundResponseSerializer.Persist(transaction, command.IdempotencyKey, fingerprint, response, timestamp);
        return response;
    }

    private static string CreateFingerprint(RoundCommand command) =>
        (command.BuyBonus ? "buy:" + BonusBuyPolicy.Version + ":" : string.Empty)
        + (command.Stake?.ToString(CultureInfo.InvariantCulture) ?? "free");

    private static long ResolveStake(
        ISessionTransaction transaction,
        RoundCommand command,
        IGameMathModel model)
    {
        if (model.Configuration.RulesetHash != transaction.Session.RulesetHash)
        {
            throw new GameApplicationException("MathVersionConflict", 409);
        }

        var isBonus = transaction.State.IsBonus;
        if (command.BuyBonus && (isBonus || transaction.Session.GameId != "thunderfall" || transaction.Session.MathVersion != "1.0.0"))
        {
            throw new GameApplicationException("BonusBuyUnavailable", 409);
        }

        if (isBonus)
        {
            var originalStake = transaction.State.Bonus!.OriginalStake;
            if (command.Stake.HasValue && command.Stake.Value != originalStake)
            {
                throw new GameApplicationException("InvalidStake");
            }

            return originalStake;
        }

        var stake = command.Stake ?? 0;
        if (!model.Configuration.Definition.Stakes.Contains(stake))
        {
            throw new GameApplicationException("InvalidStake");
        }

        return stake;
    }

    private RoundOutcome ExecuteMath(
        IGameMathModel model,
        RoundRequest request,
        GameMathState startingState,
        CancellationToken cancellationToken)
    {
        try
        {
            return model.Execute(request, startingState, random, new ExecutionOptions(CancellationToken: cancellationToken));
        }
        catch (MathExecutionException exception)
        {
            throw new GameApplicationException(exception.Message, 422);
        }
    }

    private static void ApplyAccounting(
        ISessionTransaction transaction,
        RoundCommand command,
        RoundOutcome outcome,
        long debit,
        DateTimeOffset timestamp)
    {
        if (!transaction.State.IsBonus)
        {
            AddLedgerEntry(transaction, outcome.RoundId, command.BuyBonus ? "BonusBuy" : "Stake", -debit, timestamp);
        }

        AddLedgerEntry(transaction, outcome.RoundId, "Award", outcome.FinalWin, timestamp);
        transaction.State = outcome.FinalMathState;
        transaction.Session.Version = checked(transaction.Session.Version + 1);
        transaction.Session.UpdatedAt = timestamp;
    }

    private static void AddLedgerEntry(
        ISessionTransaction transaction,
        Guid roundId,
        string type,
        long delta,
        DateTimeOffset timestamp)
    {
        transaction.Balance = checked(transaction.Balance + delta);
        transaction.Add(new WalletEntry(Guid.NewGuid(), transaction.Session.Id, roundId, type, delta, transaction.Balance, timestamp));
    }

    private static RoundResponse CreateResponse(
        ISessionTransaction transaction,
        RoundCommand command,
        RoundOutcome outcome,
        long debit,
        GameDefinition definition) =>
        new(
            outcome.RoundId,
            outcome.GameId,
            outcome.MathVersion,
            command.BuyBonus ? "BonusBuy" : outcome.Mode,
            outcome.Stake,
            outcome.InitialBoard,
            outcome.CascadeSteps,
            outcome.WinningGroups,
            outcome.MultiplierEvents,
            outcome.RawWin,
            outcome.FinalWin,
            transaction.Balance,
            transaction.State,
            command.BuyBonus ? [new("BonusIntro"), .. PresentationAdapter.Create(outcome)] : PresentationAdapter.Create(outcome),
            outcome,
            command.BuyBonus ? new BonusPurchase(BonusBuyPolicy.Version, debit, definition.FeatureRules.InitialSpins) : null,
            checked(transaction.Balance - outcome.FinalWin + debit),
            debit,
            checked(transaction.Balance - outcome.FinalWin),
            outcome.FinalWin);
}
