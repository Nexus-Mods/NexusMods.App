using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Library;

namespace NexusMods.Abstractions.Library.Installers;

/// <summary>
/// Turns <see cref="LibraryItem"/> into <see cref="LoadoutItem"/>.
/// </summary>
[PublicAPI]
public interface ILibraryItemInstaller
{
    /// <summary>
    /// Checks whether the provided library item is supported by this installer.
    /// </summary>
    /// <remarks>
    /// This method should only do surface checks on the entity itself.
    /// </remarks>
    bool IsSupportedLibraryItem(LibraryItem.ReadOnly libraryItem);

    /// <summary>
    /// Executes the installer.
    /// </summary>
    ValueTask<InstallerResult> ExecuteAsync(
        LibraryItem.ReadOnly libraryItem,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
