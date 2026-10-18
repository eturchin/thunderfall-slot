using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Engine.Abstractions.Events;

public sealed record FreeSpinsRetriggered(int SequenceNumber, int AddedSpins, BonusState State)
    : MathEvent(SequenceNumber);
