using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Library.Installers;

/// <summary>
/// Variant of <see cref="ILibraryItemInstaller"/> for <see cref="LibraryFile"/>.
/// </summary>
[PublicAPI]
public interface ILibraryFileInstaller : ILibraryItemInstaller
{
    /// <summary>
    /// Checks whether the provided library file is supported by this installer.
    /// </summary>
    ValueTask<bool> IsSupportedAsync(LibraryFile.ReadOnly libraryFile, CancellationToken cancellationToken);

    /// <summary>
    /// Executes the installer.
    /// </summary>
    ValueTask<LoadoutItem.New[]> ExecuteAsync(
        LibraryFile.ReadOnly libraryFile,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
