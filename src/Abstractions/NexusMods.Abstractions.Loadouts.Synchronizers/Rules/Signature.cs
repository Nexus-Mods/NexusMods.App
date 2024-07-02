using DynamicData.Kernel;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

[Flags]
public enum Signature : ushort
{
    /// <summary>
    /// Empty signature, used only as a way to detect an uninitialized signature.
    /// </summary>
    Empty = 0,
    
    /// <summary>
    /// True if the file exists on disk.
    /// </summary>
    DiskExists = 1,
    
    /// <summary>
    /// True if the file exists in the previous state.
    /// </summary>
    PrevExists = 2,
    
    /// <summary>
    /// True if the file exists in the loadout.
    /// </summary>
    LoadoutExists = 4,
    
    /// <summary>
    /// True if the hashes of the disk and previous state are equal.
    /// </summary>
    DiskEqualsPrev = 8,
    
    /// <summary>
    /// True if the hashes of the previous state and loadout are equal.
    /// </summary>
    PrevEqualsLoadout = 16,
    
    /// <summary>
    /// True if the hashes of the disk and loadout are equal.
    /// </summary>
    DiskEqualsLoadout = 32,
    
    /// <summary>
    /// True if the file on disk is already archived.
    /// </summary>
    DiskArchived = 64,
    
    /// <summary>
    /// True if the file in the previous state is archived.
    /// </summary>
    PrevArchived = 128,
    
    /// <summary>
    /// True if the file in the loadout is archived.
    /// </summary>
    LoadoutArchived = 256,
    
    /// <summary>
    /// True if the path is ignored, i.e. it is on a game-specific ignore list.
    /// </summary>
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
