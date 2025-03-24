using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.AdvancedInstaller;

/// <summary>
/// Advanced interactive mod installer that allows users to manually define files to install and where to install them.
/// </summary>
public class AdvancedManualInstaller : ALibraryArchiveInstaller
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

    public AdvancedManualInstaller(IServiceProvider serviceProvider, bool isDirect = false) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<AdvancedManualInstaller>>())
    {
        _handler = new Lazy<IAdvancedInstallerHandler?>(() => GetAdvancedInstallerHandler(serviceProvider, isDirect));
    }

    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        if (!IsActive) return ValueTask.FromResult<InstallerResult>(new NotSupported());
        return _handler.Value!.ExecuteAsync(libraryArchive, loadoutGroup, transaction, loadout, cancellationToken);
    }

    /// <summary>
    /// Attempts to obtain an <see cref="IAdvancedInstallerHandler"/> from the <paramref name="provider"/>.
    /// The main handler is AdvancedManualInstallerUI which might not be available if the current environment does not support UI.
    /// </summary>
    /// <returns>Null if no handler is found</returns>
    private static IAdvancedInstallerHandler? GetAdvancedInstallerHandler(IServiceProvider provider, bool isDirect)
    {
        var handler = provider.GetService<IAdvancedInstallerHandler>();
        if (handler is not null) handler.WasOpenedDirectly = isDirect;
        return handler;
    }
}
