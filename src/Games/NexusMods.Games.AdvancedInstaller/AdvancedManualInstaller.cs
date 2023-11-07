using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller;

public class AdvancedManualInstaller : AModInstaller
{
    public static AdvancedManualInstaller Create(IServiceProvider provider) => new(provider);

    private readonly Lazy<IAdvancedInstallerHandler?> _handler;

    public AdvancedManualInstaller(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _handler = new Lazy<IAdvancedInstallerHandler?>(() => GetAdvancedInstallerHandler(serviceProvider));
    }

    public bool IsActive => _handler.Value != null;

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
}
