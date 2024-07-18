using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Installers;

/// <summary>
/// Base implementation of <see cref="ILibraryFileInstaller"/>.
/// </summary>
[PublicAPI]
public abstract class ALibraryFileInstaller : ALibraryItemInstaller, ILibraryFileInstaller
{
    /// <summary>
    /// Constructor.
    /// </summary>
    protected ALibraryFileInstaller(IServiceProvider serviceProvider, ILogger logger) : base(serviceProvider, logger) { }

    /// <inheritdoc/>
    public override ValueTask<bool> IsSupportedAsync(LibraryItem.ReadOnly libraryItem, CancellationToken cancellationToken)
    {
        if (!libraryItem.TryGetAsLibraryFile(out var libraryFile)) return ValueTask.FromResult(false);
        return IsSupportedAsync(libraryFile, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual ValueTask<bool> IsSupportedAsync(LibraryFile.ReadOnly libraryFile, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(true);
    }

    /// <inheritdoc/>
    public override ValueTask<LoadoutItem.New[]> ExecuteAsync(
        LibraryItem.ReadOnly libraryItem,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        if (!libraryItem.TryGetAsLibraryFile(out var libraryFile))
        {
            Logger.LogError("The provided library item `{Name}` (`{Id}`) is not a library file!", libraryItem.Name, libraryItem.Id);
            return new ValueTask<LoadoutItem.New[]>([]);
        }

        return ExecuteAsync(libraryFile, transaction, loadout, cancellationToken);
    }

    /// <inheritdoc/>
    public abstract ValueTask<LoadoutItem.New[]> ExecuteAsync(
        LibraryFile.ReadOnly libraryFile,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
