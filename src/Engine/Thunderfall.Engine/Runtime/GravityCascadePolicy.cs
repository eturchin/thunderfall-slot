using Thunderfall.Engine.Abstractions.Contracts;
using Thunderfall.Engine.Abstractions.Definitions;
using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Runtime;

public sealed class GravityCascadePolicy : ICascadePolicy
{
    public void Apply(
        Span<Cell> board,
        ulong removed,
        CompiledDefinition definition,
        CompiledProfile profile,
        IRandomSource rng,
        ref int identity,
        List<SymbolDrop>? drops = null,
        List<SymbolSpawn>? spawns = null
    )
    {
        var rows = definition.Definition.Layout.Rows;
        for (var column = 0; column < definition.Definition.Layout.Columns; column++)
        {
            var start = column * rows;
            var write = rows - 1;
            for (var row = rows - 1; row >= 0; row--)
            {
                var from = start + row;
                if ((removed & (1UL << from)) != 0)
                {
                    continue;
                }

                var to = start + write--;
                var survivor = board[from];
                board[to] = survivor;
                if (to != from)
                {
                    drops?.Add(new(from, to, survivor));
                }
            }
            for (var row = write; row >= 0; row--)
            {
                var position = start + row;
                var cell = WeightedRefillGenerator.Draw(profile.Refill, profile, rng, ref identity);
                board[position] = cell;
                spawns?.Add(new(position, cell));
            }
        }
    }
}
