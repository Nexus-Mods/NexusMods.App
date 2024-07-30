using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Library.Installers;

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
    public override bool IsSupportedLibraryFile(LibraryFile.ReadOnly libraryFile)
    {
        if (!libraryFile.TryGetAsLibraryArchive(out var libraryArchive)) return false;
        return IsSupportedLibraryArchive(libraryArchive);
    }

    /// <inheritdoc/>
    public virtual bool IsSupportedLibraryArchive(LibraryArchive.ReadOnly libraryArchive) => true;

    /// <inheritdoc/>
    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryFile.ReadOnly libraryFile,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        if (!libraryFile.TryGetAsLibraryArchive(out var libraryArchive))
        {
            Logger.LogError("The provided library item `{Name}` (`{Id}`) is not a library archive!", libraryFile.AsLibraryItem().Name, libraryFile.Id);
            return ValueTask.FromResult<InstallerResult>(new NotSupported());
        }

        return ExecuteAsync(libraryArchive, loadoutGroup, transaction, loadout, cancellationToken);
    }

    /// <inheritdoc/>
    public abstract ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
