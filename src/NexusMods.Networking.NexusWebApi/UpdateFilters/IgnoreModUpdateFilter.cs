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
    private bool ShouldIgnoreFile(NexusModsFileMetadata.ReadOnly file) => ShouldIgnoreFile(file.Uid);

    /// <summary>Returns true if a file should be excluded from update results.</summary>
    private bool ShouldIgnoreFile(FileUid file) => _fileFilter.ShouldIgnoreFile(file);

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
        var filteredFiles = modUpdateOnPage.NewerFiles.Where(f => !ShouldIgnoreFile(f)).ToArray();

        if (filteredFiles.Length == 0)
            return null;

        return modUpdateOnPage with
        {
            NewerFiles = filteredFiles,
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
        // Filter each file mapping using the SelectMod method
        var filteredMappings = new List<ModUpdateOnPage>();

        foreach (var fileMapping in modPage.FileMappings)
        {
            var filteredMapping = SelectMod(fileMapping);
            if (filteredMapping != null)
                filteredMappings.Add(filteredMapping.Value);
        }

        // If no file mappings remain after filtering, return null to discard the entire update
        if (filteredMappings.Count == 0)
            return null;

        return new ModUpdatesOnModPage(filteredMappings.ToArray());
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
    bool ShouldIgnoreFile(FileUid file);
}

/// <summary>
/// The default implementation of the file update filter, ignoring files based on <see cref="IgnoreFileUpdate"/>(s)
/// stored in the DataStore.
/// </summary>
public class DefaultFileUpdateFilter : IShouldIgnoreFile
{
    private readonly IConnection _connection;

    /// <summary>Creates a new instance of the file update filter.</summary>
    /// <param name="connection">The database connection.</param>
    public DefaultFileUpdateFilter(IConnection connection) => _connection = connection;

    /// <inheritdoc />
    public bool ShouldIgnoreFile(FileUid file) => IgnoreFileUpdate.FindByUid(_connection.Db, file).Count > 0;
}
