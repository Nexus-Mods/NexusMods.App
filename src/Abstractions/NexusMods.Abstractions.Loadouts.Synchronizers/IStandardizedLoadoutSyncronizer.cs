using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;


/// <summary>
/// Interface for a standardized loadout syncronizer, these are pulled out from ILoadoutSynchronizer to allow
/// keep the main interface cleaner.
/// </summary>
public interface IStandardizedLoadoutSynchronizer : ILoadoutSynchronizer
{
    #region Apply Methods
    /// <summary>
    /// Converts a loadout to a flattened loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    ValueTask<FlattenedLoadout> LoadoutToFlattenedLoadout(Loadout.Model loadout);

    /// <summary>
    /// Converts a flattened loadout to a file tree.
    /// </summary>
    /// <param name="flattenedLoadout"></param>
    /// <param name="loadout"></param>
    /// <returns></returns>
    ValueTask<FileTree> FlattenedLoadoutToFileTree(FlattenedLoadout flattenedLoadout, Loadout.Model loadout);

    /// <summary>
    /// Writes a file tree to disk (updating the game files)
    /// </summary>
    /// <param name="fileTree"></param>
    /// <param name="loadout"></param>
    /// <param name="flattenedLoadout"></param>
    /// <param name="prevState"></param>
    /// <param name="installation"></param>
    /// <param name="skipIngest">
    ///     Skips checking if an ingest is needed.
    ///     Force overrides current locations to intended tree.
    /// </param>
    Task<DiskStateTree> FileTreeToDisk(FileTree fileTree, Loadout.Model loadout, FlattenedLoadout flattenedLoadout,
        DiskStateTree prevState, GameInstallation installation, bool skipIngest = false);

    #endregion

    #region Ingest Methods

    /// <summary>
    /// Indexes the game folders and creates a disk state.
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    Task<DiskStateTree> GetDiskState(GameInstallation installation);

    /// <summary>
    /// Creates a new file tree from the current disk state and the previous file tree.
    /// </summary>
    /// <param name="diskState"></param>
    /// <param name="prevLoadout"></param>
    /// <param name="prevFileTree"></param>
    /// <param name="prevDiskState"></param>
    /// <returns></returns>
    ValueTask<FileTree> DiskToFileTree(DiskStateTree diskState, Loadout.Model prevLoadout, FileTree prevFileTree, DiskStateTree prevDiskState);

    /// <summary>
    /// Creates a new flattened loadout from the current file tree and the previous flattened loadout.
    /// </summary>
    /// <param name="fileTree"></param>
    /// <param name="prevLoadout"></param>
    /// <param name="prevFlattenedLoadout"></param>
    /// <returns></returns>
    ValueTask<FlattenedLoadout> FileTreeToFlattenedLoadout(FileTree fileTree, Loadout.Model prevLoadout, FlattenedLoadout prevFlattenedLoadout);
    

    /// <summary>
    /// Backs up any new files in the file tree.
    /// </summary>
    Task BackupNewFiles(GameInstallation installation, IEnumerable<(GamePath To, Hash Hash, Size Size)> newFiles);
    #endregion
    
    
    #region File Helpers

    /// <summary>
    /// Returns true if the file is a generated file.
    /// </summary>
    bool IsGeneratedFile(File.Model file);

    /// <summary>
    /// Writes a generated file to the output stream.
    /// </summary>
    Task<Hash?> WriteGeneratedFile(File.Model file, Stream outputStream, Loadout.Model loadout, FlattenedLoadout flattenedLoadout, FileTree fileTree);


#endregion

}
