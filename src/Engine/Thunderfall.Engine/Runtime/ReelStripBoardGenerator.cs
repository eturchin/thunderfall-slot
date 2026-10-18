using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Runtime;

public sealed class ReelStripBoardGenerator : IBoardGenerator
{
    public void Generate(
        Span<Cell> board,
        CompiledDefinition definition,
        CompiledProfile profile,
        IRandomSource rng,
        ref int identity
    )
    {
        var d = definition.Definition;
        for (var column = 0; column < d.Layout.Columns; column++)
        {
            var strip = d.ReelStrips[column];
            var stop = (int)rng.NextBoundedUInt32((uint)strip.Length);
            for (var row = 0; row < d.Layout.Rows; row++)
            {
                board[d.Layout.Index(column, row)] = new(
                    strip[(stop + row) % strip.Length],
                    Identity: ++identity
                );
            }
        }
    }

    public static void GenerateWithStops(
        Span<Cell> board,
        CompiledDefinition definition,
        IRandomSource rng,
        ref int identity,
        Span<int> stops
    )
    {
        var d = definition.Definition;
        for (var c = 0; c < d.Layout.Columns; c++)
        {
            var strip = d.ReelStrips[c];
            var stop = (int)rng.NextBoundedUInt32((uint)strip.Length);
            stops[c] = stop;
            for (var r = 0; r < d.Layout.Rows; r++)
            {
                board[d.Layout.Index(c, r)] = new(
                    strip[(stop + r) % strip.Length],
                    Identity: ++identity
                );
            }
        }
    }
}
