using System.Collections.Immutable;

namespace Thunderfall.Engine.Primitives;

public sealed record Board(Layout Layout, ImmutableArray<Cell> Cells)
{
    public Cell this[int column, int row] => Cells[Layout.Index(column, row)];

    public static Board Snapshot(Layout layout, ReadOnlySpan<Cell> cells) =>
        new(layout, ImmutableArray.Create(cells.ToArray()));
}
