using System.Collections.Immutable;
using Thunderfall.Application.Contracts;
using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Events;

namespace Thunderfall.Application.Presentation;

public static class PresentationAdapter
{
    public static ImmutableArray<PresentationHint> Create(RoundOutcome outcome)
    {
        var result = ImmutableArray.CreateBuilder<PresentationHint>();
        foreach (var e in outcome.MathematicalEvents)
        {
            switch (e)
            {
                case BoardGenerated:
                    result.Add(new("Reveal"));
                    break;
                case CascadeCompleted c:
                    result.Add(new("Tumble", c.StepIndex));
                    break;
                case MultiplierCollected m:
                    result.Add(new("Lightning", Position: m.Orb.Position, Value: m.Orb.Value));
                    break;
                case FreeSpinsStarted started when started.State.Active:
                    result.Add(new("BonusIntro"));
                    break;
                case FreeSpinsRetriggered r:
                    result.Add(new("Retrigger", Value: r.AddedSpins));
                    break;
                case FeatureCompleted:
                    result.Add(new("BonusComplete"));
                    break;
                case MaxWinReached:
                    result.Add(new("MaxWin"));
                    break;
            }
        }

        if (outcome.FinalWin >= outcome.Stake * 100)
        {
            result.Add(new("MegaWin"));
        }
        else if (outcome.FinalWin >= outcome.Stake * 20)
        {
            result.Add(new("BigWin"));
        }

        return result.ToImmutable();
    }
}
