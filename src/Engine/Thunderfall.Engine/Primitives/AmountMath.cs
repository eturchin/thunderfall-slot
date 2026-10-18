namespace Thunderfall.Engine.Primitives;

public static class AmountMath
{
    public const long Scale = 100;
    public const long PayScale = 1_000_000;
    public const long InitialBalance = 1_000_000;

    // Round the complete nominal award once: integer division, ties to even. Intermediate arithmetic is Int128.
    public static long Settle(long stake, long payTicks, long multiplier)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stake);
        ArgumentOutOfRangeException.ThrowIfNegative(payTicks);
        ArgumentOutOfRangeException.ThrowIfNegative(multiplier);
        var numerator = checked((Int128)stake * payTicks * multiplier);
        var quotient = numerator / PayScale;
        var remainder = numerator % PayScale;
        if (remainder * 2 > PayScale || (remainder * 2 == PayScale && (quotient & 1) != 0))
        {
            quotient++;
        }

        return checked((long)quotient);
    }

    public static decimal Raw(long stake, long ticks) => checked((decimal)stake * ticks / PayScale);
}
