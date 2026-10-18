namespace Thunderfall.Engine.Simulation.Simulation;

public sealed class RunningMoments
{
    public long Count
    {
        get; private set;
    }
    public double Mean
    {
        get; private set;
    }
    public double M2
    {
        get; private set;
    }

    public void Add(double x)
    {
        Count++;
        var delta = x - Mean;
        Mean += delta / Count;
        M2 += delta * (x - Mean);
    }

    public void Merge(RunningMoments other)
    {
        if (other.Count == 0)
        {
            return;
        }

        if (Count == 0)
        {
            Count = other.Count;
            Mean = other.Mean;
            M2 = other.M2;
            return;
        }
        var delta = other.Mean - Mean;
        var n = checked(Count + other.Count);
        M2 += other.M2 + delta * delta * Count * other.Count / n;
        Mean += delta * other.Count / n;
        Count = n;
    }

    public double Variance => Count > 1 ? M2 / (Count - 1) : 0;
}
