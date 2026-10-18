namespace Thunderfall.Engine.Abstractions.Contracts;

public readonly record struct OrbHistogram(
    long X2,
    long X3,
    long X5,
    long X8,
    long X10,
    long X15,
    long X20,
    long X25,
    long X50,
    long X100,
    long X250,
    long X500
)
{
    public long Get(int i) =>
        i switch
        {
            0 => X2,
            1 => X3,
            2 => X5,
            3 => X8,
            4 => X10,
            5 => X15,
            6 => X20,
            7 => X25,
            8 => X50,
            9 => X100,
            10 => X250,
            11 => X500,
            _ => throw new ArgumentOutOfRangeException(nameof(i)),
        };

    public long Total => X2 + X3 + X5 + X8 + X10 + X15 + X20 + X25 + X50 + X100 + X250 + X500;
}
