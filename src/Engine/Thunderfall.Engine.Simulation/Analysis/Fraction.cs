using System.Numerics;

namespace Thunderfall.Engine.Simulation.Analysis;

public readonly record struct Fraction
{
    public BigInteger Numerator
    {
        get;
    }
    public BigInteger Denominator
    {
        get;
    }

    public Fraction(BigInteger numerator, BigInteger denominator)
    {
        if (denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator));
        }

        var gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        Numerator = numerator / gcd;
        Denominator = denominator / gcd;
    }

    public double Approximation => (double)Numerator / (double)Denominator;

    public override string ToString() => Numerator + "/" + Denominator;

    public static Fraction operator +(Fraction a, Fraction b) =>
        new(
            a.Numerator * b.Denominator + b.Numerator * a.Denominator,
            a.Denominator * b.Denominator
        );
}
