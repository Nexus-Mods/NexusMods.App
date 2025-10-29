using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Library.Installers;

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
    public override bool IsSupportedLibraryItem(LibraryItem.ReadOnly libraryItem)
    {
        if (!libraryItem.TryGetAsLibraryFile(out var libraryFile)) return false;
        return IsSupportedLibraryFile(libraryFile);
    }

    /// <inheritdoc/>
    public virtual bool IsSupportedLibraryFile(LibraryFile.ReadOnly libraryFile) => true;

    /// <inheritdoc/>
    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryItem.ReadOnly libraryItem,
        LoadoutItemGroup.New loadoutGroup,
        Transaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        if (!libraryItem.TryGetAsLibraryFile(out var libraryFile))
        {
            Logger.LogError("The provided library item `{Name}` (`{Id}`) is not a library file!", libraryItem.Name, libraryItem.Id);
            return ValueTask.FromResult<InstallerResult>(new NotSupported("The provided library item is not a library file"));
        }

        return ExecuteAsync(libraryFile, loadoutGroup, transaction, loadout, cancellationToken);
    }

    /// <inheritdoc/>
    public abstract ValueTask<InstallerResult> ExecuteAsync(
        LibraryFile.ReadOnly libraryFile,
        LoadoutItemGroup.New loadoutGroup,
        Transaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
