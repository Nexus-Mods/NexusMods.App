using System.Diagnostics;
using static NexusMods.Abstractions.Loadouts.Synchronizers.Rules.Actions;
using static NexusMods.Abstractions.Loadouts.Synchronizers.Rules.SignatureShorthand;

namespace NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

public class ActionMapping
{

    /// <summary>
    /// Maps a signature to the corresponding actions
    /// </summary>
    public static Actions MapActions(Signature signature)
    {
        return MapActions((SignatureShorthand)signature);
    }
    
    /// <summary>
    /// Maps a shorthand signature to the corresponding actions
    /// </summary>
    public static Actions MapActions(SignatureShorthand shorthand)
    {
        Debug.Assert(Enum.IsDefined(shorthand), $"Unknown value: {shorthand} ({(int)shorthand})");
        
        // Format of the shorthand:
        // xxx_yyy_z -> xxx: Loadout, yyy: Archive, z: Ignore path
        // xxx: a tuple of `(Disk, Previous, Loadout)` states.
        // A `x` means no state because that source has no value for the path.
        // A `A`, `B`, `C` are placeholders for the hash of the file, so `AAA` means all three sources have the same hash, while `BAA` means the hash is different on disk
        // from either the previous state or the loadout.
        // yyy: a tuple of `(Disk, Previous, Loadout)` archive states. `XXX` means all three sources are archived (regardless of their hash) and `Xxx` means the disk is archived but the previous and loadout states are not.
        // `z`: either `i` or `I`, where `i` means the path is not ignored and `I` means the path is ignored.
        // The easiest way to think of this is that a capital letter means the existence of data, while a lowercase letter means the absence of data or a false value.
        return shorthand switch
        {
            xxA_xxx_i => WarnOfUnableToExtract,
            xxA_xxX_i => ExtractToDisk,
            xxA_xxx_I => WarnOfUnableToExtract,
            xxA_xxX_I => ExtractToDisk,
            xAx_xxx_i => DoNothing,
            xAx_xXx_i => DoNothing,
            xAx_xxx_I => DoNothing,
            xAx_xXx_I => DoNothing,
            xAA_xxx_i => WarnOfUnableToExtract,
            xAA_xXX_i => AddReifiedDelete,
            xAA_xxx_I => AddReifiedDelete,
            xAA_xXX_I => AddReifiedDelete,
            xAB_xxx_i => WarnOfUnableToExtract,
            xAB_xXx_i => WarnOfUnableToExtract,
            xAB_xxX_i => ExtractToDisk,
            xAB_xXX_i => ExtractToDisk,
            xAB_xxx_I => WarnOfUnableToExtract,
            xAB_xXx_I => WarnOfUnableToExtract,
            xAB_xxX_I => ExtractToDisk,
            xAB_xXX_I => ExtractToDisk,
            Axx_xxx_i => BackupFile | IngestFromDisk,
            Axx_Xxx_i => IngestFromDisk,
            Axx_xxx_I => IngestFromDisk,
            Axx_Xxx_I => IngestFromDisk,
            AxA_xxx_i => BackupFile,
            AxA_XxX_i => DoNothing,
            AxA_xxx_I => DoNothing,
            AxA_XxX_I => DoNothing,
            AxB_xxx_i => BackupFile | IngestFromDisk,
            AxB_Xxx_i => IngestFromDisk,
            AxB_xxX_i => BackupFile | IngestFromDisk,
            AxB_XxX_i => IngestFromDisk,
            AxB_xxx_I => BackupFile | IngestFromDisk,
            AxB_Xxx_I => BackupFile | IngestFromDisk,
            AxB_xxX_I => BackupFile | DeleteFromDisk | ExtractToDisk,
            AxB_XxX_I => BackupFile | DeleteFromDisk | ExtractToDisk,
            AAx_xxx_i => BackupFile | DeleteFromDisk,
            AAx_XXx_i => DeleteFromDisk,
            AAx_xxx_I => BackupFile | DeleteFromDisk,
            AAx_XXx_I => DeleteFromDisk,
            AAA_xxx_i => BackupFile,
            AAA_XXX_i => DoNothing,
            AAA_xxx_I => DoNothing,
            AAA_XXX_I => DoNothing,
            AAB_xxx_i => WarnOfUnableToExtract,
            AAB_XXx_i => WarnOfUnableToExtract,
            AAB_xxX_i => BackupFile | DeleteFromDisk | ExtractToDisk,
            AAB_XXX_i => DeleteFromDisk | ExtractToDisk,
            AAB_xxx_I => WarnOfUnableToExtract,
            AAB_XXx_I => WarnOfUnableToExtract,
            AAB_xxX_I => BackupFile | DeleteFromDisk | ExtractToDisk,
            AAB_XXX_I => DeleteFromDisk | ExtractToDisk,
            ABx_xxx_i => BackupFile | DeleteFromDisk,
            ABx_Xxx_i => DeleteFromDisk,
            ABx_xXx_i => BackupFile | DeleteFromDisk,
            ABx_XXx_i => DeleteFromDisk,
            ABx_xxx_I => BackupFile | DeleteFromDisk,
            ABx_Xxx_I => DeleteFromDisk,
            ABx_xXx_I => BackupFile | DeleteFromDisk,
            ABx_XXx_I => DeleteFromDisk,
            ABA_xxx_i => BackupFile,
            ABA_XxX_i => DoNothing,
            ABA_xXx_i => BackupFile,
            ABA_XXX_i => DoNothing,
            ABA_xxx_I => DoNothing,
            ABA_XxX_I => DoNothing,
            ABA_xXx_I => DoNothing,
            ABA_XXX_I => DoNothing,
            ABB_xxx_i => BackupFile | IngestFromDisk,
            ABB_Xxx_i => IngestFromDisk,
            ABB_xXX_i => BackupFile | IngestFromDisk,
            ABB_XXX_i => IngestFromDisk,
            ABB_xxx_I => BackupFile | IngestFromDisk,
            ABB_Xxx_I => IngestFromDisk,
            ABB_xXX_I => BackupFile | IngestFromDisk,
            ABB_XXX_I => IngestFromDisk,
            ABC_xxx_i => WarnOfConflict,
            ABC_Xxx_i => WarnOfUnableToExtract,
            ABC_xXx_i => WarnOfConflict,
            ABC_xxX_i => BackupFile | IngestFromDisk,
            ABC_XXx_i => BackupFile | IngestFromDisk,
            ABC_XxX_i => WarnOfConflict,
            ABC_xXX_i => BackupFile | IngestFromDisk,
            ABC_XXX_i => IngestFromDisk,
            ABC_xxx_I => BackupFile | IngestFromDisk,
            ABC_Xxx_I => WarnOfUnableToExtract,
            ABC_xXx_I => WarnOfUnableToExtract,
            ABC_xxX_I => BackupFile | DeleteFromDisk | ExtractToDisk,
            ABC_XXx_I => WarnOfUnableToExtract,
            ABC_XxX_I => DeleteFromDisk | ExtractToDisk,
            ABC_xXX_I => BackupFile | DeleteFromDisk | ExtractToDisk,
            ABC_XXX_I => DeleteFromDisk | ExtractToDisk
        };
    }
}
