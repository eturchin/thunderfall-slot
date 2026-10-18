using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Engine.Abstractions.Events;

public sealed record FeatureCompleted(int SequenceNumber, BonusState State)
    : MathEvent(SequenceNumber);
