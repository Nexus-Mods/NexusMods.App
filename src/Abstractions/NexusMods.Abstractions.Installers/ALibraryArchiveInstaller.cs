using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Installers;

/// <summary>
/// Base implementation of <see cref="ILibraryArchiveInstaller"/>.
/// </summary>
[PublicAPI]
public abstract class ALibraryArchiveInstaller : ALibraryFileInstaller, ILibraryArchiveInstaller
{
    /// <summary>
    /// Constructor.
    /// </summary>
    protected ALibraryArchiveInstaller(IServiceProvider serviceProvider, ILogger logger) : base(serviceProvider, logger) { }

    /// <inheritdoc/>
    public override ValueTask<bool> IsSupportedAsync(LibraryFile.ReadOnly libraryFile, CancellationToken cancellationToken)
    {
        if (!libraryFile.TryGetAsLibraryArchive(out var libraryArchive)) return ValueTask.FromResult(false);
        return IsSupportedAsync(libraryArchive, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask<bool> IsSupportedAsync(LibraryArchive.ReadOnly libraryArchive, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(true);
    }

    /// <inheritdoc/>
    public override ValueTask<LoadoutItem.New[]> ExecuteAsync(
        LibraryFile.ReadOnly libraryFile,
        ITransaction transaction,
        LoadoutDetails loadoutDetails,
        CancellationToken cancellationToken)
    {
        if (!libraryFile.TryGetAsLibraryArchive(out var libraryArchive))
        {
            Logger.LogError("The provided library item `{Name}` (`{Id}`) is not a library archive!", libraryFile.AsLibraryItem().Name, libraryFile.Id);
            return new ValueTask<LoadoutItem.New[]>([]);
        }

        return ExecuteAsync(libraryArchive, transaction, loadoutDetails, cancellationToken);
    }

    /// <inheritdoc/>
    public abstract ValueTask<LoadoutItem.New[]> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        ITransaction transaction,
        LoadoutDetails loadoutDetails,
        CancellationToken cancellationToken);
}
