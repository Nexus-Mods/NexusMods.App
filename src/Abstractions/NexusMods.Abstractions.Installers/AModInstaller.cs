namespace NexusMods.Abstractions.Installers;

/// <summary>
///     Mod installer base class that provides support for the installation of mods
/// </summary>
public abstract class AModInstaller : IModInstaller
{
    /// <summary>
    ///     Helper for returning no results
    /// </summary>
    public static readonly IEnumerable<ModInstallerResult> NoResults = Enumerable.Empty<ModInstallerResult>();

    // Not used yet, but here to force the service provider to be injected by implementing classes
    // ReSharper disable once NotAccessedField.Local
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected AModInstaller(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public abstract ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default);
}
