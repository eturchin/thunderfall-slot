using Thunderfall.Engine.Primitives;
using Thunderfall.Engine.Randomness;

namespace Thunderfall.Engine.Abstractions.Definitions;

public static class DefinitionValidator
{
    public static void Validate(GameDefinition definition)
    {
        void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new ArgumentException("ConfigurationError: " + message);
            }
        }
        Require(
            !string.IsNullOrWhiteSpace(definition.GameId)
                && !string.IsNullOrWhiteSpace(definition.MathVersion)
                && System.Text.RegularExpressions.Regex.IsMatch(
                    definition.GameId,
                    @"^[a-z0-9]+(?:-[a-z0-9]+)*$"
                )
                && System.Text.RegularExpressions.Regex.IsMatch(
                    definition.MathVersion,
                    @"^[0-9]+\.[0-9]+\.[0-9]+(?:-[a-zA-Z0-9.-]+)?$"
                ),
            "identity/semantic version"
        );
        Require(
            definition.Layout.Columns is >= 1 and <= 8
                && definition.Layout.Rows is >= 1 and <= 8
                && definition.Layout.Size <= 64,
            "layout"
        );
        Require(definition.Kind is "CountAnywhere" or "Paylines", "kind");
        Require(
            !definition.Symbols.IsDefaultOrEmpty
                && definition.Symbols.Distinct().Count() == definition.Symbols.Length
                && definition.Symbols.All(s => s > SymbolId.Empty && s <= SymbolId.Wild),
            "symbols"
        );
        Require(
            definition.Stakes.Length > 0
                && definition.Stakes.Distinct().Count() == definition.Stakes.Length
                && definition.Stakes.All(s => s > 0 && s <= 10000000),
            "stakes"
        );
        Require(
            definition.MaxWinMultiplier is >= 1 and <= 100000
                && definition.ResourceLimits.MaxCascades is >= 1 and <= 100000
                && definition.ResourceLimits.MaxBonusRounds > 0,
            "limits"
        );
        Require(
            definition.Paytable.Count > 0
                && definition.Paytable.All(p =>
                    p.Key >= SymbolId.Crown
                    && p.Key <= SymbolId.YellowGem
                    && definition.Symbols.Contains(p.Key)
                    && p.Value.Length > 0
                    && p.Value.Select(b => b.MinimumCount)
                        .SequenceEqual(p.Value.Select(b => b.MinimumCount).Order())
                    && p.Value.Select(b => b.MinimumCount).Distinct().Count() == p.Value.Length
                    && p.Value.All(b =>
                        b.MinimumCount > 0
                        && b.MinimumCount <= definition.Layout.Size
                        && b.PayTicks > 0
                        && b.PayTicks <= 1000 * AmountMath.PayScale
                    )
                ),
            "paytable"
        );
        foreach (var p in new[] { definition.BaseGenerationProfile, definition.BonusGenerationProfile })
        {
            foreach (var weights in new[] { p.InitialWeights, p.RefillWeights })
            {
                Require(
                    weights.Length > 0
                        && weights.Select(w => w.Symbol).Distinct().Count() == weights.Length
                        && weights.All(w => definition.Symbols.Contains(w.Symbol)),
                    "weights"
                );
                _ = new WeightedSampler(weights.Select(w => ((int)w.Symbol, w.Weight)));
            }
            Require(
                p.OrbValues.Length > 0
                    && p.OrbValues.All(w =>
                        w.Value
                            is 2
                                or 3
                                or 5
                                or 8
                                or 10
                                or 15
                                or 20
                                or 25
                                or 50
                                or 100
                                or 250
                                or 500
                    )
                    && p.OrbValues.Select(w => w.Value).Distinct().Count() == p.OrbValues.Length,
                "orb values"
            );
            _ = new WeightedSampler(p.OrbValues.Select(w => (w.Value, w.Weight)));
        }
        if (definition.Kind == "CountAnywhere")
        {
            Require(
                definition.FeatureRules.TriggerScatters >= 4
                    && definition.FeatureRules.InitialSpins > 0
                    && definition.FeatureRules.RetriggerScatters >= 3
                    && definition.FeatureRules.RetriggerSpins > 0,
                "features"
            );
            var p = definition.BonusGenerationProfile.InitialWeights;
            var scatter =
                (double)p.Where(w => w.Symbol == SymbolId.ThunderGate).Sum(w => (long)w.Weight)
                / p.Sum(w => (long)w.Weight);
            // Union bound P(K>=k)<=choose(n,k)*p^k; reject clearly supercritical bonus candidates conservatively.
            double choose = 1;
            for (var k = 1; k <= definition.FeatureRules.RetriggerScatters; k++)
            {
                choose *= (double)(definition.Layout.Size - k + 1) / k;
            }

            Require(
                definition.FeatureRules.RetriggerSpins
                    * choose
                    * System.Math.Pow(scatter, definition.FeatureRules.RetriggerScatters)
                    < 1,
                "bonus subcriticality bound"
            );
            Require(
                p.Any(w => w.Symbol == SymbolId.ThunderGate && w.Weight > 0),
                "positive initial scatter mass"
            );
            foreach (var profile in new[] { definition.BaseGenerationProfile, definition.BonusGenerationProfile })
            {
                Require(
                    profile.RefillWeights.Any(w =>
                        (w.Symbol is SymbolId.StormOrb or SymbolId.ThunderGate) && w.Weight > 0
                    ),
                    "positive terminating refill special mass"
                );
            }
        }
        else
        {
            Require(
                definition.ReelStrips.Length == definition.Layout.Columns
                    && definition.ReelStrips.All(r =>
                        r.Length >= definition.Layout.Rows && r.All(definition.Symbols.Contains)
                    ),
                "strips"
            );
            Require(
                definition.Paylines.Length > 0
                    && definition.Paylines.Select(l => string.Join(',', l)).Distinct().Count()
                        == definition.Paylines.Length
                    && definition.Paylines.All(l =>
                        l.Length == definition.Layout.Columns && l.All(r => r >= 0 && r < definition.Layout.Rows)
                    ),
                "paylines"
            );
        }
    }
}
