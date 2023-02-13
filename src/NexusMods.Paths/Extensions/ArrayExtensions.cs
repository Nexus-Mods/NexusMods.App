namespace NexusMods.Paths.Extensions;

/// <summary>
/// Various extensions related to working with arrays.
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// Verifies if two slices of an array are equal.
    /// </summary>
    public static bool AreEqual<T>(T[]? a, int startA, T[]? b, int startB, int length)
        where T : IEquatable<T>
    {
        if (startA + length > (a?.Length ?? 0)) return false;
        if (startB + length > (b?.Length ?? 0)) return false;

        for (var i = 0; i < length; i++)
            if (!a![startA + i].Equals(b![startB + i]))
                return false;
        
        return true;
    }
    
    /// <summary>
    /// Verifies if two slices of string arrays are equal, ignoring case.
    /// </summary>
    public static bool AreEqualIgnoreCase(string[]? a, int startA, string[]? b, int startB, int length)
    {
        if (startA + length > (a?.Length ?? 0)) return false;
        if (startB + length > (b?.Length ?? 0)) return false;

        for (var i = 0; i < length; i++)
            if (!a![startA + i].Equals(b![startB + i], StringComparison.InvariantCultureIgnoreCase))
                return false;
        
        return true;
    }

    /// <summary>
    /// Compares two arrays for sorting purposes.
    /// </summary>
    public static int Compare<T>(T[] a, T[] b)
        where T : IComparable<T>
    {
        var idx = 0;
        while (true)
        {
            if (idx == a.Length && idx == b.Length) return 0;
            if (idx == a.Length && idx < b.Length) return -1;
            if (idx == b.Length && idx < a.Length) return 1;

            var comp = a[idx].CompareTo(b[idx]);
            if (comp != 0) return comp;
            idx++;
        }
    }

    /// <summary>
    /// Compares two string arrays for sorting purposes, ignoring case.
    /// </summary>
    public static int CompareString(string[] a, string[] b)
    {
        var idx = 0;
        while (true)
        {
            if (idx == a.Length && idx == b.Length) return 0;
            if (idx == a.Length && idx < b.Length) return -1;
            if (idx == b.Length && idx < a.Length) return 1;

            var comp = string.Compare(a[idx], b[idx], StringComparison.InvariantCultureIgnoreCase);
            if (comp != 0) return comp;
            idx++;
        }
    }
}