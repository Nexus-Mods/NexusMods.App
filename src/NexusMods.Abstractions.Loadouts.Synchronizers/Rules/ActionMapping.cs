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
            xxA_xxx_iL => WarnOfUnableToExtract,
            xxA_xxX_iL => ExtractToDisk,
            xxA_xxx_IL => WarnOfUnableToExtract,
            xxA_xxX_IL => ExtractToDisk,
            xAx_xxx_iL => DoNothing,
            xAx_xXx_iL => DoNothing,
            xAx_xxx_IL => DoNothing,
            xAx_xXx_IL => DoNothing,
            xAA_xxx_iL => WarnOfUnableToExtract,
            xAA_xXX_iL => AddReifiedDelete,
            xAA_xxx_IL => AddReifiedDelete,
            xAA_xXX_IL => AddReifiedDelete,
            xAB_xxx_iL => WarnOfUnableToExtract,
            xAB_xXx_iL => WarnOfUnableToExtract,
            xAB_xxX_iL => ExtractToDisk,
            xAB_xXX_iL => ExtractToDisk,
            xAB_xxx_IL => WarnOfUnableToExtract,
            xAB_xXx_IL => WarnOfUnableToExtract,
            xAB_xxX_IL => ExtractToDisk,
            xAB_xXX_IL => ExtractToDisk,
            Axx_xxx_iL => BackupFile | IngestFromDisk,
            Axx_Xxx_iL => IngestFromDisk,
            Axx_xxx_IL => IngestFromDisk,
            Axx_Xxx_IL => IngestFromDisk,
            AxA_xxx_iL => BackupFile,
            AxA_XxX_iL => DoNothing,
            AxA_xxx_IL => DoNothing,
            AxA_XxX_IL => DoNothing,
            AxB_xxx_iL => BackupFile | IngestFromDisk,
            AxB_Xxx_iL => IngestFromDisk,
            AxB_xxX_iL => BackupFile | IngestFromDisk,
            AxB_XxX_iL => IngestFromDisk,
            AxB_xxx_IL => BackupFile | IngestFromDisk,
            AxB_Xxx_IL => BackupFile | IngestFromDisk,
            AxB_xxX_IL => BackupFile | DeleteFromDisk | ExtractToDisk,
            AxB_XxX_IL => BackupFile | DeleteFromDisk | ExtractToDisk,
            AAx_xxx_iL => BackupFile | DeleteFromDisk,
            AAx_XXx_iL => DeleteFromDisk,
            AAx_xxx_IL => BackupFile | DeleteFromDisk,
            AAx_XXx_IL => DeleteFromDisk,
            AAA_xxx_iL => DoNothing,
            AAA_XXX_iL => DoNothing,
            AAA_xxx_IL => DoNothing,
            AAA_XXX_IL => DoNothing,
            AAB_xxx_iL => WarnOfUnableToExtract,
            AAB_XXx_iL => WarnOfUnableToExtract,
            AAB_xxX_iL => BackupFile | DeleteFromDisk | ExtractToDisk,
            AAB_XXX_iL => DeleteFromDisk | ExtractToDisk,
            AAB_xxx_IL => WarnOfUnableToExtract,
            AAB_XXx_IL => WarnOfUnableToExtract,
            AAB_xxX_IL => BackupFile | DeleteFromDisk | ExtractToDisk,
            AAB_XXX_IL => DeleteFromDisk | ExtractToDisk,
            ABx_xxx_iL => BackupFile | DeleteFromDisk,
            ABx_Xxx_iL => DeleteFromDisk,
            ABx_xXx_iL => BackupFile | DeleteFromDisk,
            ABx_XXx_iL => DeleteFromDisk,
            ABx_xxx_IL => BackupFile | DeleteFromDisk,
            ABx_Xxx_IL => DeleteFromDisk,
            ABx_xXx_IL => BackupFile | DeleteFromDisk,
            ABx_XXx_IL => DeleteFromDisk,
            ABA_xxx_iL => BackupFile,
            ABA_XxX_iL => DoNothing,
            ABA_xXx_iL => BackupFile,
            ABA_XXX_iL => DoNothing,
            ABA_xxx_IL => DoNothing,
            ABA_XxX_IL => DoNothing,
            ABA_xXx_IL => DoNothing,
            ABA_XXX_IL => DoNothing,
            ABB_xxx_iL => BackupFile | IngestFromDisk,
            ABB_Xxx_iL => IngestFromDisk,
            ABB_xXX_iL => BackupFile | IngestFromDisk,
            ABB_XXX_iL => IngestFromDisk,
            ABB_xxx_IL => BackupFile | IngestFromDisk,
            ABB_Xxx_IL => IngestFromDisk,
            ABB_xXX_IL => BackupFile | IngestFromDisk,
            ABB_XXX_IL => IngestFromDisk,
            ABC_xxx_iL => WarnOfConflict,
            ABC_Xxx_iL => WarnOfUnableToExtract,
            ABC_xXx_iL => WarnOfConflict,
            ABC_xxX_iL => BackupFile | IngestFromDisk,
            ABC_XXx_iL => BackupFile | IngestFromDisk,
            ABC_XxX_iL => WarnOfConflict,
            ABC_xXX_iL => BackupFile | IngestFromDisk,
            ABC_XXX_iL => IngestFromDisk,
            ABC_xxx_IL => BackupFile | IngestFromDisk,
            ABC_Xxx_IL => WarnOfUnableToExtract,
            ABC_xXx_IL => WarnOfUnableToExtract,
            ABC_xxX_IL => BackupFile | DeleteFromDisk | ExtractToDisk,
            ABC_XXx_IL => WarnOfUnableToExtract,
            ABC_XxX_IL => DeleteFromDisk | ExtractToDisk,
            ABC_xXX_IL => BackupFile | DeleteFromDisk | ExtractToDisk,
            ABC_XXX_IL => DeleteFromDisk | ExtractToDisk
        };
    }
}
