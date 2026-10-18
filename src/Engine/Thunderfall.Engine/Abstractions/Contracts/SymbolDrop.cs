using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Abstractions.Contracts;

public sealed record SymbolDrop(int From, int To, Cell Cell);
