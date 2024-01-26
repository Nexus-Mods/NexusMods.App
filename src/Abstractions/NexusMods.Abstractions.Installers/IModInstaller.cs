using JetBrains.Annotations;

namespace NexusMods.Abstractions.Installers;

/// <summary>
///     Game specific extension that provides support for the installation of mods
///     (currently archives) to the game folder.
/// </summary>
[PublicAPI]
public interface IModInstaller
{
    /// <summary>
    ///     Finds all mods inside the provided archive.
    /// </summary>
    /// <param name="info">Information for the mod installer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of found mods inside this archive.</returns>
    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default);
}
