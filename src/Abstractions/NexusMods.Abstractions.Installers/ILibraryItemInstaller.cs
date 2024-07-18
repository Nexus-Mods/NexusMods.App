using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Installers;

/// <summary>
/// Turns <see cref="LibraryItem"/> into <see cref="LoadoutItem"/>.
/// </summary>
[PublicAPI]
public interface ILibraryItemInstaller
{
    /// <summary>
    /// Checks whether the provided library item is supported by this installer.
    /// </summary>
    ValueTask<bool> IsSupportedAsync(LibraryItem.ReadOnly libraryItem, CancellationToken cancellationToken);

    /// <summary>
    /// Executes the installer.
    /// </summary>
    ValueTask<LoadoutItem.New[]> ExecuteAsync(
        LibraryItem.ReadOnly libraryItem,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
