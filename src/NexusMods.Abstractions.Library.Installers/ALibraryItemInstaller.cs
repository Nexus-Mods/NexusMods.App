using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Library.Installers;

/// <summary>
/// Base implementation of <see cref="ILibraryItemInstaller"/>.
/// </summary>
[PublicAPI]
public abstract class ALibraryItemInstaller : ILibraryItemInstaller
{
    /// <summary>
    /// Service provider.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;

    /// <summary>
    /// Logger.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ALibraryItemInstaller(
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
    }

    /// <inheritdoc/>
    public virtual bool IsSupportedLibraryItem(LibraryItem.ReadOnly libraryItem) => true;

    /// <inheritdoc/>
    public abstract ValueTask<InstallerResult> ExecuteAsync(
        LibraryItem.ReadOnly libraryItem,
        LoadoutItemGroup.New loadoutGroup,
        Transaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
