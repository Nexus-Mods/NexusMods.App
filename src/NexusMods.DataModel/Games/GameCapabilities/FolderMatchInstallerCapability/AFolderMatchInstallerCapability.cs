
namespace NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;

/// <summary>
///    GameCapability for games that support installing mod archives by matching archive folder to a known folder structure.
/// </summary>
public abstract class AFolderMatchInstallerCapability : IGameCapability
{
    /// <inheritdoc />
    public static GameCapabilityId CapabilityId =>
        GameCapabilityId.From(new Guid("71F525D2-B4FB-4350-A6AA-0D5F4091E9BB"));


    /// <summary>
    /// Collection of InstallFolderTargets to provide to the installer.
    /// Reimplement this property to contain the InstallFolderTargets for a specific game or collection of games.
    /// </summary>
    public abstract IEnumerable<InstallFolderTarget> InstallFolderTargets();

}
