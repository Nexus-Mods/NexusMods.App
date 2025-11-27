using DynamicData.Kernel;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

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
    public Optional<LoadoutItemGroupId> ParentGroupId { get; }
    
    /// <summary>
    /// The optional installer to use (if null, the library will choose the best installer)
    /// </summary>
    public ILibraryItemInstaller? Installer { get; }
}

/// <summary>
/// The result of installing a loadout item via the <see cref="IInstallLoadoutItemJob"/>.
/// This struct holds a <see cref="LoadoutItemGroup"/> for the item which was just installed.
///
/// If the value is 'null' then the job was attached to an existing, external transaction
/// to be part of a larger atomic operation.
/// (Done by passing an <see cref="ITransaction"/> transaction to the job.)
/// This is because the value is not yet available; as the transaction
/// needs to be externally committed by the caller.
/// </summary>
public record struct InstallLoadoutItemJobResult(LoadoutItemGroup.ReadOnly? LoadoutItemGroup, LoadoutItemGroupId GroupTxId);
