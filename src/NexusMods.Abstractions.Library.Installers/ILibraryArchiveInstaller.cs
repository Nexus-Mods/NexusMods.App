using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Models.Library;

namespace NexusMods.Abstractions.Library.Installers;

/// <summary>
/// Variant of <see cref="ILibraryFileInstaller"/> for <see cref="LibraryArchive"/>
/// </summary>
[PublicAPI]
public interface ILibraryArchiveInstaller : ILibraryFileInstaller
{
    /// <summary>
    /// Checks whether the provided library archive is supported by this installer.
    /// </summary>
    /// <remarks>
    /// This method should only do surface checks on the entity itself.
    /// </remarks>
    bool IsSupportedLibraryArchive(LibraryArchive.ReadOnly libraryArchive);

    /// <summary>
    /// Executes the installer.
    /// </summary>
    ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken);
}
