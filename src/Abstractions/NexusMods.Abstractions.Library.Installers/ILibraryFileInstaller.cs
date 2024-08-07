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
    /// <remarks>
    /// This method should only do surface checks on the entity itself.
    /// </remarks>
    bool IsSupportedLibraryFile(LibraryFile.ReadOnly libraryFile);

    /// <summary>
    /// Executes the installer.
    /// </summary>
    ValueTask<InstallerResult> ExecuteAsync(
        LibraryFile.ReadOnly libraryFile,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
