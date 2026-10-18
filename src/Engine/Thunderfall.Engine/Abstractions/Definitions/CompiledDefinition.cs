using Thunderfall.Engine.Primitives;

namespace Thunderfall.Engine.Abstractions.Definitions;

public sealed class CompiledDefinition
{
    public GameDefinition Definition
    {
        get;
    }
    public string RulesetHash
    {
        get;
    }
    public CompiledProfile Base
    {
        get;
    }
    public CompiledProfile Bonus
    {
        get;
    }
    private readonly long[,] _payouts;

    public CompiledDefinition(GameDefinition definition)
    {
        DefinitionValidator.Validate(definition);
        Definition = definition;
        RulesetHash = definition.RulesetHash;
        Base = new(definition.BaseGenerationProfile);
        Bonus = new(definition.BonusGenerationProfile);
        _payouts = new long[12, definition.Layout.Size + 1];
        foreach (var (id, bands) in definition.Paytable)
        {
            for (var count = 0; count <= definition.Layout.Size; count++)
            {
                foreach (var band in bands)
                {
                    if (count >= band.MinimumCount)
                    {
                        _payouts[(int)id, count] = band.PayTicks;
                    }
                }
            }
        }
    }

    public long Payout(SymbolId id, int count) => _payouts[(int)id, count];
}
