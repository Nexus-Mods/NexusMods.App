using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.LoadoutSynchronizer;

/// <summary>
/// A Loadout Synchronizer is responsible for synchronizing loadouts between to and from the game folder.
/// </summary>
public interface ILoadoutSynchronizer
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
    Task<DiskState> FileTreeToDisk(FileTree fileTree, DiskState prevState, GameInstallation installation);

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
    /// <param name="prevFileTree"></param>
    /// <returns></returns>
    FileTree DiskToFileTree(DiskState diskState, FileTree prevFileTree);

    /// <summary>
    /// Creates a new flattened loadout from the current file tree and the previous flattened loadout.
    /// </summary>
    /// <param name="fileTree"></param>
    /// <param name="prevFlattenedLoadout"></param>
    /// <returns></returns>
    FlattenedLoadout FileTreeToFlattenedLoadout(FileTree fileTree, FlattenedLoadout prevFlattenedLoadout);

    /// <summary>
    /// Creates a new loadout from the current flattened loadout and the previous loadout.
    /// </summary>
    /// <param name="flattenedLoadout"></param>
    /// <param name="prevLoadout"></param>
    /// <returns></returns>
    Loadout FlattenedLoadoutToLoadout(FlattenedLoadout flattenedLoadout, Loadout prevLoadout);

    #endregion

    #region Merge Methods

    /// <summary>
    /// Merges two loadouts together, creating a new loadout.
    /// </summary>
    /// <param name="loadoutA"></param>
    /// <param name="loadoutB"></param>
    /// <returns></returns>
    Loadout MergeLoadouts(Loadout loadoutA, Loadout loadoutB);

    #endregion

    #region High Level Methods

    /// <summary>
    /// Applies a loadout to the game folder.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns>The new DiskState after the files were applied</returns>
    Task<DiskState> Apply(Loadout loadout);


    /// <summary>
    /// Ingests changes from the game folder into the loadout.
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    Task Ingest(Loadout loadout);

    Task<Loadout> Manage(GameInstallation installation);

    #endregion



}
