namespace Thunderfall.Engine.Randomness;

public static class RandomBounds
{
    public static uint Next(IRandomSource source, uint bound)
    {
        ArgumentOutOfRangeException.ThrowIfZero(bound);
        var threshold = unchecked(0u - bound) % bound;
        uint word;
        do
        {
            word = (uint)source.NextUInt64();
        } while (word < threshold);
        return word % bound;
    }
}
