namespace Thunderfall.Engine.Primitives;

public sealed record Layout(int Columns, int Rows)
{
    public int Size => checked(Columns * Rows);

    public int Index(int column, int row) => checked(column * Rows + row);
}
