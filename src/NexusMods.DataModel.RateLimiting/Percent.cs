namespace NexusMods.DataModel.RateLimiting;

public readonly struct Percent : IComparable, IEquatable<Percent>
{
    public static readonly Percent One = new(1d);
    public static readonly Percent Zero = new(0d);

    public readonly double Value;
    public Percent Inverse => new(1d - Value, false);

    private Percent(double d, bool check)
    {
        if (!check || InRange(d))
            Value = d;
        else
            throw new ArgumentException("Element out of range: " + d);
    }

    public Percent(double d)
        : this(d, true)
    {
    }

    public static bool InRange(double d)
    {
        return d is >= 0 and <= 1;
    }

    public static Percent operator +(Percent c1, Percent c2)
    {
        return new Percent(c1.Value + c2.Value);
    }
    public static Percent operator -(Percent c1, Percent c2)
    {
        return new Percent(c1.Value - c2.Value);
    }

    public static bool operator ==(Percent c1, Percent c2)
    {
        return c1.Value == c2.Value;
    }

    public static bool operator !=(Percent c1, Percent c2)
    {
        return c1.Value != c2.Value;
    }

    public static bool operator >(Percent c1, Percent c2)
    {
        return c1.Value > c2.Value;
    }

    public static bool operator <(Percent c1, Percent c2)
    {
        return c1.Value < c2.Value;
    }

    public static bool operator >=(Percent c1, Percent c2)
    {
        return c1.Value >= c2.Value;
    }

    public static bool operator <=(Percent c1, Percent c2)
    {
        return c1.Value <= c2.Value;
    }

    public static explicit operator double(Percent c1)
    {
        return c1.Value;
    }

    public static Percent FactoryPutInRange(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) throw new ArgumentException();
        if (d < 0)
            return Zero;
        if (d > 1) return One;
        return new Percent(d, false);
    }

    public static Percent FactoryPutInRange(int cur, int max)
    {
        return FactoryPutInRange(1.0d * cur / max);
    }

    public static Percent FactoryPutInRange(long cur, long max)
    {
        return FactoryPutInRange(1.0d * cur / max);
    }

    public override bool Equals(object? obj)
    {
        if (!(obj is Percent rhs)) return false;
        return Equals(rhs);
    }

    public bool Equals(Percent other)
    {
        return Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return ToString(0);
    }

    public string ToString(string format)
    {
        return $"{(Value * 100).ToString(format)}%";
    }

    public string ToString(int numDigits)
    {
        return ToString("n" + numDigits);
    }

    public int CompareTo(object? obj)
    {
        if (obj is Percent rhs) return Value.CompareTo(rhs.Value);
        return 0;
    }

    public static bool TryParse(string str, out Percent p)
    {
        if (double.TryParse(str, out var d))
        {
            d /= 100;
            if (InRange(d))
            {
                p = new Percent(d);
                return true;
            }
        }


        p = default;
        return false;
    }
}
