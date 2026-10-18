using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Abstractions.Events;
using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Runtime;

internal static class SettlementEventWriter
{
    public static void Append(List<MathEvent> events, ref int sequence, GameDefinition definition, RoundRequest request, GameMathState state, FeatureTransition transition, long ticks, long collected, int scatters)
    {
        var bonus = transition.State.Bonus;
        if (state.IsBonus && collected > 0)
        {
            events!.Add(new BonusMultiplierUpdated(sequence++, state.Bonus!.PersistentMultiplier, collected, transition.EffectiveMultiplier));
        }

        if (transition.Triggered || transition.Retriggered)
        {
            events!.Add(new ScatterTriggerDetected(sequence++, scatters, transition.Retriggered));
        }

        if (transition.Triggered)
        {
            events!.Add(new FreeSpinsStarted(sequence++, bonus!));
        }

        if (transition.Retriggered)
        {
            events!.Add(new FreeSpinsRetriggered(sequence++, definition.FeatureRules.RetriggerSpins, bonus!));
        }

        if (state.IsBonus)
        {
            events!.Add(new FreeSpinCompleted(sequence++, bonus!));
        }

        events!.Add(new RoundWinCalculated(sequence++, ticks, AmountMath.Raw(request.Stake, ticks), transition.EffectiveMultiplier, transition.NominalAward, transition.Award));
        if (transition.CapReached)
        {
            events.Add(new MaxWinReached(sequence++, transition.NominalAward, transition.Award, (state.IsBonus ? state.Bonus!.CycleAward : 0) + transition.Award, transition.AbandonedSpins));
        }

        if (transition.Completed || (transition.CapReached && bonus is not null))
        {
            events.Add(new FeatureCompleted(sequence++, bonus!));
        }
    }
}
