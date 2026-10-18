namespace Thunderfall.Engine.Primitives;

public readonly record struct Cell(
    SymbolId Symbol,
    int Multiplier = 0,
    int Identity = 0,
    bool Collected = false
);
