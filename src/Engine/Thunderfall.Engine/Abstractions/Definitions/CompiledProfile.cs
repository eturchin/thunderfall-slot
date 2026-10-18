using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Abstractions.Definitions;

public sealed class CompiledProfile
{
    public WeightedSampler Initial
    {
        get;
    }
    public WeightedSampler Refill
    {
        get;
    }
    public WeightedSampler OrbValues
    {
        get;
    }

    public CompiledProfile(GenerationProfile p)
    {
        Initial = new(p.InitialWeights.Select(x => ((int)x.Symbol, x.Weight)));
        Refill = new(p.RefillWeights.Select(x => ((int)x.Symbol, x.Weight)));
        OrbValues = new(p.OrbValues.Select(x => (x.Value, x.Weight)));
    }
}
