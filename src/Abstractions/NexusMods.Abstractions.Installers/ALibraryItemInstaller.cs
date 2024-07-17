using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Installers;

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
    public virtual ValueTask<bool> IsSupportedAsync(LibraryItem.ReadOnly libraryItem, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(true);
    }

    /// <inheritdoc/>
    public abstract ValueTask<LoadoutItem.New[]> ExecuteAsync(
        LibraryItem.ReadOnly libraryItem,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
