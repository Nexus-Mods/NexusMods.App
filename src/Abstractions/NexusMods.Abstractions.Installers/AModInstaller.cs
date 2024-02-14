using JetBrains.Annotations;

namespace NexusMods.Abstractions.Installers;

/// <summary>
/// Mod installer base class that provides support for the installation of mods
/// </summary>
[PublicAPI]
public abstract class AModInstaller : IModInstaller
{
    /// <summary>
    /// Return no results.
    /// </summary>
    protected static readonly IEnumerable<ModInstallerResult> NoResults = Enumerable.Empty<ModInstallerResult>();

    /// <summary>
    /// Service Provider.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected AModInstaller(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public abstract ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default);
}
