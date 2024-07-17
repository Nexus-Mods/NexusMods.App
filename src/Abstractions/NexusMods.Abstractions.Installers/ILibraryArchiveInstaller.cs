using JetBrains.Annotations;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Installers;

/// <summary>
/// Variant of <see cref="ILibraryFileInstaller"/> for <see cref="LibraryArchive"/>
/// </summary>
[PublicAPI]
public interface ILibraryArchiveInstaller : ILibraryFileInstaller
{
    /// <summary>
    /// Checks whether the provided library archive is supported by this installer.
    /// </summary>
    ValueTask<bool> IsSupportedAsync(LibraryArchive.ReadOnly libraryArchive, CancellationToken cancellationToken);

    /// <summary>
    /// Executes the installer.
    /// </summary>
    ValueTask<LoadoutItem.New[]> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
