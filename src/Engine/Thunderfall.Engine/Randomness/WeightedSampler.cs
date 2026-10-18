namespace Thunderfall.Engine.Randomness;

public sealed class WeightedSampler
{
    private readonly uint[] _cumulative;
    private readonly int[] _values;
    public uint Total
    {
        get;
    }

    public WeightedSampler(IEnumerable<(int Value, uint Weight)> entries)
    {
        var all = entries.ToArray();
        if (all.Length == 0)
        {
            throw new ArgumentException("Empty weights");
        }

        _cumulative = new uint[all.Length];
        _values = new int[all.Length];
        uint total = 0;
        for (var i = 0; i < all.Length; i++)
        {
            total = checked(total + all[i].Weight);
            _cumulative[i] = total;
            _values[i] = all[i].Value;
        }
        if (total == 0)
        {
            throw new ArgumentException("Zero weight total");
        }

        Total = total;
    }

    public int Sample(IRandomSource rng)
    {
        var draw = rng.NextBoundedUInt32(Total);
        var lo = 0;
        var hi = _cumulative.Length - 1;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (draw < _cumulative[mid])
            {
                hi = mid;
            }
            else
            {
                lo = mid + 1;
            }
        }
        return _values[lo];
    }
}
