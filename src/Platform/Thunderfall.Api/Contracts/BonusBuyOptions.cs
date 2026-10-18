namespace Thunderfall.Api.Contracts;

public sealed record BonusBuyOptions(string PolicyVersion, long PriceNumerator, long PriceDenominator, int InitialSpins);
