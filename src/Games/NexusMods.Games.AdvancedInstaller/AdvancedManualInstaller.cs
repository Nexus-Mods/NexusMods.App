using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller;

/// <summary>
/// Advanced interactive mod installer that allows users to manually define files to install and where to install them.
/// </summary>
public class AdvancedManualInstaller : AModInstaller
{
    private readonly Lazy<IAdvancedInstallerHandler?> _handler;

    /// <summary>
    /// Whether a handler for this installer is available in the current environment.
    /// E.g. no UI available during CLI execution.
    /// </summary>
    public bool IsActive => _handler.Value != null;

    /// <summary>
    /// Creates a new instance of <see cref="AdvancedManualInstaller"/> given the provided <paramref name="provider"/>.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static AdvancedManualInstaller Create(IServiceProvider provider) => new(provider);

    public AdvancedManualInstaller(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _handler = new Lazy<IAdvancedInstallerHandler?>(() => GetAdvancedInstallerHandler(serviceProvider));
    }

    public override ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        // No UI -> fail install.
        if (!IsActive)
        {
            return new ValueTask<IEnumerable<ModInstallerResult>>(Enumerable.Empty<ModInstallerResult>());
        }

        return _handler.Value!.GetModsAsync(gameInstallation, loadoutId, baseModId, archiveFiles, cancellationToken);
    }


    /// <summary>
    /// Attempts to obtain an <see cref="IAdvancedInstallerHandler"/> from the <paramref name="provider"/>.
    /// The main handler is AdvancedManualInstallerUI which might not be available if the current environment does not support UI.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns>Null if no handler is found</returns>
    private static IAdvancedInstallerHandler? GetAdvancedInstallerHandler(IServiceProvider provider)
    {
        try
        {
            return provider.GetRequiredService<IAdvancedInstallerHandler>();
        }
        catch (InvalidOperationException)
        {
            // No handler registered -> no UI.
            return null;
        }
    }
}
