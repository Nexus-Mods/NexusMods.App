using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Networking.NexusWebApi.UpdateFilters;

/// <summary>
/// A class whose purpose is to hide updates for <see cref="ModUpdateService"/>.
/// </summary>
public class IgnoreModUpdateFilter
{
    private readonly IConnection _connection;
    
    /// <summary/>
    public IgnoreModUpdateFilter(IConnection connection) => _connection = connection;

    /// <summary>Returns true if a file should be excluded from update results.</summary>
    internal bool ShouldIgnoreFile(NexusModsFileMetadata.ReadOnly file) => ShouldIgnoreFile(file.Uid);

    /// <summary>Returns true if a file should be excluded from update results.</summary>
    internal bool ShouldIgnoreFile(UidForFile file) => IgnoreFileUpdateModel.FindByUid(_connection.Db, file).Count > 0;

    /// <summary>
    /// Plugs into <see cref="ModUpdateService.GetNewestFileVersionObservable"/>
    /// </summary>
    /// <param name="modPage">
    ///     The update info for the mod on a mod page.
    ///     We can either mutate the update info, or return 'null' to discard the update data.
    /// </param>
    /// <returns>The modified update data, or 'null' to discard the update data entirely</returns>
    public ModUpdateOnPage? SelectMod(ModUpdateOnPage modPage)
    {
        return modPage with
        {
            NewerFiles = modPage.NewerFiles.Where(f => !ShouldIgnoreFile(f)).ToArray(),
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
