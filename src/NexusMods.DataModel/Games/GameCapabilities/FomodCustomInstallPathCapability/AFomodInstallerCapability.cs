using NexusMods.Paths;

namespace NexusMods.DataModel.Games.GameCapabilities.FomodCustomInstallPathCapability;


/// <summary>
/// GameCapability for games that require a custom install path for fomod installers.
/// By default fomod installer will install to the Game Root folder.
/// </summary>
public abstract class AFomodCustomInstallPathCapability : IGameCapability
{
    /// <inheritdoc/>
    public static GameCapabilityId CapabilityId =>
        GameCapabilityId.From(new Guid("0F2A9DC6-1EDD-4716-B0A6-535D34F58F4F"));

    /// <summary>
    /// Defines the GamePath where mod files should be installed to.
    /// </summary>
    /// <returns></returns>
    public abstract GamePath ModInstallationPath();
}
