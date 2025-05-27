using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Networking.NexusWebApi.UpdateFilters;

/// <summary>
/// A class whose purpose is to hide updates for <see cref="ModUpdateService"/>.
/// </summary>
public class IgnoreModUpdateFilter<TShouldIgnoreFile> where TShouldIgnoreFile : IShouldIgnoreFile
{
    private readonly TShouldIgnoreFile _fileFilter;

    /// <summary>Creates a new instance of the file update filter.</summary>
    /// <param name="fileFilter">Implementation of the method to check if a file should be filtered.</param>
    public IgnoreModUpdateFilter(TShouldIgnoreFile fileFilter) => _fileFilter = fileFilter;

    /// <summary>Returns true if a file should be excluded from update results.</summary>
    internal bool ShouldIgnoreFile(NexusModsFileMetadata.ReadOnly file) => ShouldIgnoreFile(file.Uid);

    /// <summary>Returns true if a file should be excluded from update results.</summary>
    internal bool ShouldIgnoreFile(UidForFile file) => _fileFilter.ShouldIgnoreFile(file);

    /// <summary>
    /// Plugs into <see cref="ModUpdateService.GetNewestFileVersionObservable"/>
    /// </summary>
    /// <param name="modUpdateOnPage">
    ///     The update info for the mod on a mod page.
    ///     We can either mutate the update info, or return 'null' to discard the update data.
    /// </param>
    /// <returns>The modified update data, or 'null' to discard the update data entirely</returns>
    public ModUpdateOnPage? SelectMod(ModUpdateOnPage modUpdateOnPage)
    {
        return modUpdateOnPage with
        {
            NewerFiles = modUpdateOnPage.NewerFiles.Where(f => !ShouldIgnoreFile(f)).ToArray(),
        };
    }
    
    /// <summary>
    /// Plugs into <see cref="ModUpdateService.GetNewestModPageVersionObservable"/>
    /// </summary>
    /// <param name="modPage">
    ///     The update info for the mod page.
    ///     We can either mutate the update info, or return 'null' to discard the update data.
    /// </param>
    /// <returns>The modified update data, or 'null' to discard the update data entirely</returns>
    public ModUpdatesOnModPage? SelectModPage(ModUpdatesOnModPage modPage)
    {
        return null;
    }
}

/// <summary>
/// The default implementation of <see cref="IgnoreModUpdateFilter{TFilter}"/> using <see cref="DefaultFileUpdateFilter"/>
/// (DataStore) as the backend.
/// </summary>
public class IgnoreModUpdateFilter : IgnoreModUpdateFilter<DefaultFileUpdateFilter>
{
    /// <summary>
    /// Creates a new instance of the update filter using the default file filter implementation.
    /// </summary>
    public IgnoreModUpdateFilter(IConnection connection) 
        : base(new DefaultFileUpdateFilter(connection))
    {
    }
}

/// <summary>
/// A filter interface for determining if a file should be ignored in update checks.
/// The 'ignored' in this context means that the file will not be suggested as a new version.
/// </summary>
public interface IShouldIgnoreFile
{
    /// <summary>Returns true if a file should be excluded from update results.</summary>
    bool ShouldIgnoreFile(UidForFile file);
}

/// <summary>
/// The default implementation of the file update filter, ignoring files based on <see cref="IgnoreFileUpdateModel"/>(s)
/// stored in the DataStore.
/// </summary>
public class DefaultFileUpdateFilter : IShouldIgnoreFile
{
    private readonly IConnection _connection;
    
    /// <summary>Creates a new instance of the file update filter.</summary>
    /// <param name="connection">The database connection.</param>
    public DefaultFileUpdateFilter(IConnection connection) => _connection = connection;

    /// <inheritdoc />
    public bool ShouldIgnoreFile(UidForFile file) => IgnoreFileUpdateModel.FindByUid(_connection.Db, file).Count > 0;
}
