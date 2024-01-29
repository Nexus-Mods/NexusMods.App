using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;

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
    ValueTask<FlattenedLoadout> LoadoutToFlattenedLoadout(Loadout loadout);

    /// <summary>
    /// Converts a flattened loadout to a file tree.
    /// </summary>
    /// <param name="flattenedLoadout"></param>
    /// <param name="loadout"></param>
    /// <returns></returns>
    ValueTask<FileTree> FlattenedLoadoutToFileTree(FlattenedLoadout flattenedLoadout, Loadout loadout);

    /// <summary>
    /// Writes a file tree to disk (updating the game files)
    /// </summary>
    /// <param name="fileTree"></param>
    /// <param name="loadout"></param>
    /// <param name="flattenedLoadout"></param>
    /// <param name="prevState"></param>
    /// <param name="installation"></param>
    Task<DiskState> FileTreeToDisk(FileTree fileTree, Loadout loadout, FlattenedLoadout flattenedLoadout,
        DiskState prevState, GameInstallation installation);

    #endregion

    #region Ingest Methods

    /// <summary>
    /// Indexes the game folders and creates a disk state.
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
    Task<DiskState> GetDiskState(GameInstallation installation);

    /// <summary>
    /// Creates a new file tree from the current disk state and the previous file tree.
    /// </summary>
    /// <param name="diskState"></param>
    /// <param name="prevLoadout"></param>
    /// <param name="prevFileTree"></param>
    /// <param name="prevDiskState"></param>
    /// <returns></returns>
    ValueTask<FileTree> DiskToFileTree(DiskState diskState, Loadout prevLoadout, FileTree prevFileTree, DiskState prevDiskState);

    /// <summary>
    /// Creates a new flattened loadout from the current file tree and the previous flattened loadout.
    /// </summary>
    /// <param name="fileTree"></param>
    /// <param name="prevLoadout"></param>
    /// <param name="prevFlattenedLoadout"></param>
    /// <returns></returns>
    ValueTask<FlattenedLoadout> FileTreeToFlattenedLoadout(FileTree fileTree, Loadout prevLoadout, FlattenedLoadout prevFlattenedLoadout);

    /// <summary>
    /// Creates a new loadout from the current flattened loadout and the previous loadout.
    /// </summary>
    /// <param name="flattenedLoadout"></param>
    /// <param name="prevLoadout"></param>
    /// <param name="prevFlattenedLoadout"></param>
    /// <returns></returns>
    ValueTask<Loadout> FlattenedLoadoutToLoadout(FlattenedLoadout flattenedLoadout, Loadout prevLoadout, FlattenedLoadout prevFlattenedLoadout);

    /// <summary>
    /// Backs up any new files in the file tree.
    /// </summary>
    /// <param name="loadout"></param>
    /// <param name="fileTree"></param>
    /// <returns></returns>
    Task BackupNewFiles(Loadout loadout, FileTree fileTree);
    #endregion
}
