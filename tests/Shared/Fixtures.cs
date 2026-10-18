using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;
using Thunderfall.Engine.Runtime;

namespace Thunderfall.Tests;

public static class Fixtures
{
    public static GameDefinition Definition(string game = "thunderfall") =>
        DefinitionJson.Load(
            Path.Combine(AppContext.BaseDirectory, "configs", game, "v1", "game.json")
        );

    public static GameDefinition InitialDefinition() =>
        DefinitionJson.Load(
            Path.Combine(AppContext.BaseDirectory, "configs/thunderfall/v1/candidate-001.json")
        );

    public static RoundEngine Model() => new(Definition());

    public static Cell[] WinningBoard()
    {
        var cells = Enumerable
            .Range(0, 30)
            .Select(i => new Cell((SymbolId)(2 + i % 7), Identity: i + 1))
            .ToArray();
        for (var i = 0; i < 8; i++)
        {
            cells[i] = new(SymbolId.Crown, Identity: i + 1);
        }

        cells[8] = new(SymbolId.StormOrb, 2, 9);
        return cells;
    }

    public sealed class FixedGenerator(Cell[] initial) : IBoardGenerator
    {
        public void Generate(
            Span<Cell> board,
            CompiledDefinition definition,
            CompiledProfile profile,
            IRandomSource rng,
            ref int identity
        )
        {
            initial.CopyTo(board);
            identity = initial.Max(c => c.Identity);
        }
    }

    public sealed class RefillRandom(int crowns = 0) : IRandomSource
    {
        private int call;

        public ulong NextUInt64() => ulong.MaxValue;

        public uint NextBoundedUInt32(uint bound) => call++ < crowns ? 0 : bound - 1;
    }

    public static string Json<T>(T value) =>
        System.Text.Json.JsonSerializer.Serialize(value, DefinitionJson.Options);
}
