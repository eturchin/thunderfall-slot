using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Thunderfall.Engine.Randomness;

public sealed class CryptoRandomSource : IRandomSource
{
    public ulong NextUInt64()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }

    public uint NextBoundedUInt32(uint bound) => RandomBounds.Next(this, bound);
}
