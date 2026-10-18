using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Thunderfall.Engine.Randomness;

// Independently implemented from Blackman/Vigna public-domain reference algorithms.
public sealed class Xoshiro256StarStar : IRandomSource
{
    public const string Version = "xoshiro256ss-splitmix64-cycle-v1";
    public const ulong Gamma = 0x9E3779B97F4A7C15UL;
    private ulong _state0;
    private ulong _state1;
    private ulong _state2;
    private ulong _state3;

    public Xoshiro256StarStar(ulong seed) => Reset(seed);

    public static ulong Mix(ulong x)
    {
        unchecked
        {
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }

    public static ulong Split(ref ulong seed)
    {
        seed = unchecked(seed + Gamma);
        return Mix(seed);
    }

    public void Reset(ulong seed)
    {
        _state0 = Split(ref seed);
        _state1 = Split(ref seed);
        _state2 = Split(ref seed);
        _state3 = Split(ref seed);
        if ((_state0 | _state1 | _state2 | _state3) == 0)
        {
            _state0 = Gamma;
        }
    }

    public ulong NextUInt64()
    {
        unchecked
        {
            var result = BitOperations.RotateLeft(_state1 * 5, 7) * 9;
            var t = _state1 << 17;
            _state2 ^= _state0;
            _state3 ^= _state1;
            _state1 ^= _state2;
            _state0 ^= _state3;
            _state2 ^= t;
            _state3 = BitOperations.RotateLeft(_state3, 45);
            return result;
        }
    }

    public uint NextBoundedUInt32(uint bound) => RandomBounds.Next(this, bound);

    public static ulong Domain(string gameId, string mathVersion) =>
        BinaryPrimitives.ReadUInt64LittleEndian(
            SHA256.HashData(Encoding.UTF8.GetBytes(gameId + "@" + mathVersion))
        );

    public const int ChunkSize = 4096;

    public static ulong CycleSeed(ulong seed, ulong streamId, long paidRoundIndex, ulong domain)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(paidRoundIndex);
        var chunk = (ulong)paidRoundIndex / ChunkSize;
        var offset = (ulong)paidRoundIndex % ChunkSize;
        var chunkSeed = Mix(
            seed ^ domain ^ Mix(unchecked(streamId + Gamma)) ^ Mix(unchecked(chunk + 2 * Gamma))
        );
        return Mix(unchecked(chunkSeed + Gamma * (offset + 1)));
    }
}
