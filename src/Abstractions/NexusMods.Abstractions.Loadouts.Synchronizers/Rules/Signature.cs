using DynamicData.Kernel;
using NexusMods.Hashing.xxHash3;

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
    DiskExists = 1 << 0,
    
    /// <summary>
    /// True if the file exists in the previous state.
    /// </summary>
    PrevExists = 1 << 1,
    
    /// <summary>
    /// True if the file exists in the loadout.
    /// </summary>
    LoadoutExists = 1 << 2,
    
    /// <summary>
    /// True if the hashes of the disk and previous state are equal.
    /// </summary>
    DiskEqualsPrev = 1 << 3,
    
    /// <summary>
    /// True if the hashes of the previous state and loadout are equal.
    /// </summary>
    PrevEqualsLoadout = 1 << 4,
    
    /// <summary>
    /// True if the hashes of the disk and loadout are equal.
    /// </summary>
    DiskEqualsLoadout = 1 << 5,
    
    /// <summary>
    /// True if the file on disk is already archived.
    /// </summary>
    DiskArchived = 1 << 6,
    
    /// <summary>
    /// True if the file in the previous state is archived.
    /// </summary>
    PrevArchived = 1 << 7,
    
    /// <summary>
    /// True if the file in the loadout is archived.
    /// </summary>
    LoadoutArchived = 1 << 8,
    
    /// <summary>
    /// True if the path is ignored, i.e. it is on a game-specific ignore list.
    /// </summary>
    PathIsIgnored = 1 << 9,
}


/// <summary>
/// A builder for creating a <see cref="Signature"/> from its components, assign the properties and call <see cref="Build"/> to get the final <see cref="Signature"/>.
/// </summary>
public readonly struct SignatureBuilder
{
    /// <summary>
    /// The hash of the file on disk.
    /// </summary>
    public Optional<Hash> DiskHash { get; init; }
    
    /// <summary>
    /// The hash of the file in the previous state.
    /// </summary>
    public Optional<Hash> PrevHash { get; init; }
    
    /// <summary>
    /// The hash of the file in the loadout.
    /// </summary>
    public Optional<Hash> LoadoutHash { get; init; }
    
    /// <summary>
    /// True if the file on disk is already archived.
    /// </summary>
    public bool DiskArchived { get; init; }
    
    /// <summary>
    /// True if the file in the previous state is archived.
    /// </summary>
    public bool PrevArchived { get; init; }
    
    /// <summary>
    /// True if the file in the loadout is archived.
    /// </summary>
    public bool LoadoutArchived { get; init; }
    
    /// <summary>
    /// True if the path is ignored, i.e. it is on a game-specific ignore list.
    /// </summary>
    public bool PathIsIgnored { get; init; }

    /// <summary>
    /// Builds the final <see cref="Signature"/> from the properties.
    /// </summary>
    /// <returns></returns>
    public Signature Build()
    {
        var sig = Signature.Empty;

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
