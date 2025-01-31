using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Library.Jobs;

/// <summary>
/// A job that installs a library item to a loadout
/// </summary>
public interface IInstallLoadoutItemJob : IJobDefinition<InstallLoadoutItemJobResult>
{
    /// <summary>
    /// The library item to install
    /// </summary>
    public LibraryItem.ReadOnly LibraryItem { get; }
    
    /// <summary>
    /// The target loadout
    /// </summary>
    public LoadoutId LoadoutId { get; }
    
    /// <summary>
    /// The target parent group id
    /// </summary>
    public LoadoutItemGroupId ParentGroupId { get; }
    
    /// <summary>
    /// The optional installer to use (if null, the library will choose the best installer)
    /// </summary>
    public ILibraryItemInstaller? Installer { get; }
}

/// <summary>
/// The result of installing a loadout item via the <see cref="IInstallLoadoutItemJob"/>.
/// This struct holds a <see cref="LoadoutItemGroup"/> for the item which was just installed.
///
/// If the value is 'null' then the job was attached to an existing, external transaction,
/// by passing a <see cref="ITransaction"/> transaction to the job then the value will be null
/// (default) as the value remains unavailable until changes are externally committed.
/// </summary>
public record struct InstallLoadoutItemJobResult(LoadoutItemGroup.ReadOnly? LoadoutItemGroup);
