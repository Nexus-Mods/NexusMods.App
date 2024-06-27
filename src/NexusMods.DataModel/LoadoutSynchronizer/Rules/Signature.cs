using DynamicData.Kernel;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.LoadoutSynchronizer.Rules;

[Flags]
public enum Signature : ushort
{
    Empty = 0,
    DiskExists = 1,
    PrevExists = 2,
    LoadoutExists = 4,
    DiskEqualsPrev = 8,
    PrevEqualsLoadout = 16,
    DiskEqualsLoadout = 32,
    DiskArchived = 64,
    PrevArchived = 128,
    LoadoutArchived = 256,
    PathIsIgnored = 512,
}


public struct SignatureBuilder
{
    public Optional<Hash> DiskHash { get; init; }
    
    public Optional<Hash> PrevHash { get; init; }
    
    public Optional<Hash> LoadoutHash { get; init; }
    
    public bool DiskArchived { get; init; }
    
    public bool PrevArchived { get; init; }
    
    public bool LoadoutArchived { get; init; }
    
    public bool PathIsIgnored { get; init; }

    public Signature Build()
    {
        Signature sig = Signature.Empty;

        if (DiskHash.HasValue)
            sig |= Signature.DiskExists;
        
        if (PrevHash.HasValue)
            sig |= Signature.PrevExists;
        
        if (LoadoutHash.HasValue)
            sig |= Signature.LoadoutExists;

        if (DiskHash.HasValue && PrevHash.HasValue && DiskHash.Value == PrevHash.Value)
            sig |= Signature.DiskEqualsPrev;
        
        if (PrevHash.HasValue && LoadoutHash.HasValue && PrevHash.Value == LoadoutHash.Value)
            sig |= Signature.PrevEqualsLoadout;
        
        if (DiskHash.HasValue && LoadoutHash.HasValue && DiskHash.Value == LoadoutHash.Value)
            sig |= Signature.DiskEqualsLoadout;

        if (DiskArchived)
            sig |= Signature.DiskArchived;
        
        if (PrevArchived)
            sig |= Signature.PrevArchived;
        
        if (LoadoutArchived)
            sig |= Signature.LoadoutArchived;

        if (PathIsIgnored)
            sig |= Signature.PathIsIgnored;

        return sig;
    }
}
