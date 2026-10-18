using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Features;

public sealed class SettlementRule : IFeatureRule
{
    public FeatureTransition Evaluate(
        CompiledDefinition compiled,
        RoundRequest request,
        GameMathState state,
        int scatters,
        long collected,
        long payTicks
    )
    {
        var definition = compiled.Definition;
        var previous = state.IsBonus ? state.Bonus : null;
        var isFree = previous is not null;
        var effective = isFree
            ? checked(previous!.PersistentMultiplier + collected)
            : System.Math.Max(1, collected);
        var nominal = AmountMath.Settle(request.Stake, payTicks, effective);
        var prior = previous?.CycleAward ?? 0;
        var cap = checked(request.Stake * definition.MaxWinMultiplier);
        var remaining = checked(cap - prior);
        if (remaining < 0)
        {
            throw new MathExecutionException("InvalidCycleState");
        }

        var award = System.Math.Min(nominal, remaining);
        var cycle = checked(prior + award);
        var capped = cycle >= cap;
        var trigger =
            !isFree && definition.Kind == "CountAnywhere" && scatters >= definition.FeatureRules.TriggerScatters;
        var retrigger = isFree && scatters >= definition.FeatureRules.RetriggerScatters;
        BonusState? next = null;
        var abandoned = 0;
        if (isFree)
        {
            var spins = checked(
                previous!.RemainingSpins - 1 + (retrigger ? definition.FeatureRules.RetriggerSpins : 0)
            );
            next = previous with
            {
                RemainingSpins = spins,
                PlayedSpins = checked(previous.PlayedSpins + 1),
                PersistentMultiplier = effective,
                AccumulatedWin = checked(previous.AccumulatedWin + award),
                RetriggerCount = checked(previous.RetriggerCount + (retrigger ? 1 : 0)),
                CycleAward = cycle,
            };
        }
        else if (trigger)
        {
            next = new(
                request.RoundId,
                definition.GameId,
                definition.MathVersion,
                request.Stake,
                definition.FeatureRules.InitialSpins,
                0,
                1,
                0,
                0,
                1,
                cycle
            );
        }

        if (capped && next is not null)
        {
            abandoned = next.RemainingSpins;
            next = next with
            {
                RemainingSpins = 0
            };
        }
        return new(
            next is null ? GameMathState.Empty : new(next),
            award,
            nominal,
            effective,
            capped,
            abandoned,
            trigger,
            retrigger,
            isFree && next?.RemainingSpins == 0
        );
    }
}
