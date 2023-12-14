using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Trees;
using NexusMods.Paths;
using NexusMods.Paths.Trees;

namespace NexusMods.DataModel.ModInstallers;

/// <summary>
/// Mod installer base class that provides support for the installation of mods
/// </summary>
public abstract class AModInstaller : IModInstaller
{
    // Not used yet, but here to force the service provider to be injected by implementing classes
    // ReSharper disable once NotAccessedField.Local
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected AModInstaller(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    /// <summary>
    /// Helper for returning no results
    /// </summary>
    public static readonly IEnumerable<ModInstallerResult> NoResults = Enumerable.Empty<ModInstallerResult>();

    /// <inheritdoc />
    public abstract ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        KeyedBox<RelativePath, ModFileTree> archiveFiles,
        CancellationToken cancellationToken = default);
}
