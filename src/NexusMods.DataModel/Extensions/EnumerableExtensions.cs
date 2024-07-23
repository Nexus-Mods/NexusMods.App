using DynamicData.Kernel;

namespace NexusMods.DataModel.Extensions;

/// <summary>
/// Extensions for <see cref="IEnumerable{T}"/>.
/// </summary>
public static class EnumerableExtensions
{

    /// <summary>
    /// Joins two collections using a comparator function, assumes both collections are sorted the same way, if a match is not found, the value is Optional.None()
    /// for the missing side
    /// </summary>
    public static IEnumerable<(Optional<TA>, Optional<TB>)> MergeJoin<TA, TB>(IEnumerable<TA> aColl, IEnumerable<TB> bColl, Func<TA, TB, int> comparator) 
        where TA : notnull 
        where TB : notnull
    {
        
        using var aEnumerator = aColl.GetEnumerator();
        using var bEnumerator = bColl.GetEnumerator();

        var aHasNext = aEnumerator.MoveNext();
        var bHasNext = bEnumerator.MoveNext();

        while (aHasNext && bHasNext)
        {
            var aCurrent = aEnumerator.Current;
            var bCurrent = bEnumerator.Current;

            var comparison = comparator(aCurrent, bCurrent);

            switch (comparison)
            {
                case < 0:
                    yield return (Optional<TA>.None, bCurrent);
                    bHasNext = bEnumerator.MoveNext();
                    break;
                case > 0:
                    yield return (aCurrent, Optional<TB>.None);
                    aHasNext = aEnumerator.MoveNext();
                    break;
                default:
                    yield return (aCurrent, bCurrent);
                    aHasNext = aEnumerator.MoveNext();
                    bHasNext = bEnumerator.MoveNext();
                    break;
            }
        }

        while (aHasNext)
        {
            yield return (aEnumerator.Current, Optional<TB>.None);
            aHasNext = aEnumerator.MoveNext();
        }

        while (bHasNext)
        {
            yield return (Optional<TA>.None, bEnumerator.Current);
            bHasNext = bEnumerator.MoveNext();
        }
    }
    
}
