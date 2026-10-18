namespace Thunderfall.Engine.Randomness;

public interface IRandomSource
{
    ulong NextUInt64();
    uint NextBoundedUInt32(uint exclusiveUpperBound);
}
