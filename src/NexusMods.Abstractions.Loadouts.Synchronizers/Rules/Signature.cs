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
public static class SignatureBuilder
{
    /// <summary>
    /// Builds the final <see cref="Signature"/> from the arguments.
    /// </summary>
    /// <returns></returns>
    public static Signature Build(Optional<Hash> diskHash, Optional<Hash> prevHash, Optional<Hash> loadoutHash, bool diskArchived, bool prevArchived, bool loadoutArchived, bool pathIsIgnored)
    {
        var sig = Signature.Empty;

        if (diskHash.HasValue)
            sig |= Signature.DiskExists;
        
        if (prevHash.HasValue)
            sig |= Signature.PrevExists;
        
        if (loadoutHash.HasValue)
            sig |= Signature.LoadoutExists;

        if (diskHash.HasValue && prevHash.HasValue && diskHash.Value == prevHash.Value)
            sig |= Signature.DiskEqualsPrev;
        
        if (prevHash.HasValue && loadoutHash.HasValue && prevHash.Value == loadoutHash.Value)
            sig |= Signature.PrevEqualsLoadout;
        
        if (diskHash.HasValue && loadoutHash.HasValue && diskHash.Value == loadoutHash.Value)
            sig |= Signature.DiskEqualsLoadout;

        if (diskArchived)
            sig |= Signature.DiskArchived;
        
        if (prevArchived)
            sig |= Signature.PrevArchived;
        
        if (loadoutArchived)
            sig |= Signature.LoadoutArchived;

        if (pathIsIgnored)
            sig |= Signature.PathIsIgnored;

        return sig;
    }
}
