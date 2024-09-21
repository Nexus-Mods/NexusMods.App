using DynamicData.Kernel;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

public abstract class ThreeWayMerger<TA, TB, TC, TOut>
    where TA : notnull
    where TB : notnull
    where TC : notnull
{
    public abstract int CompareAB(TA a, TB b);
    public abstract int CompareAC(TA a, TC c);
    public abstract int CompareBC(TB b, TC c);
    
    public abstract TOut Combine(Optional<TA> a, Optional<TB> b, Optional<TC> c);

    public void Merge(IEnumerable<TA> aColl, IEnumerable<TB> bColl, IEnumerable<TC> cColl)
    {
        
    }
    
}
