namespace Thunderfall.Engine.Abstractions.Contracts;

public sealed record GameMathState(BonusState? Bonus = null)
{
    public bool IsBonus => Bonus?.Active == true;
    public static GameMathState Empty { get; } = new();
}
