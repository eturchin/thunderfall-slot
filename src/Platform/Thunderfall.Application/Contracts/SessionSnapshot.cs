using Thunderfall.Application.Entities;
using Thunderfall.Engine.Abstractions.Contracts;

namespace Thunderfall.Application.Contracts;

public sealed record SessionSnapshot(GameSession Session, long Balance, GameMathState State);
