using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Engine.Abstractions.Events;

public sealed record MultiplierCollected(int SequenceNumber, OrbCollection Orb)
    : MathEvent(SequenceNumber);
