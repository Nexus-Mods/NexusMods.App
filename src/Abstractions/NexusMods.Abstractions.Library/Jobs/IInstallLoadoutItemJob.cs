using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Library.Jobs;

/// <summary>
/// A job that installs a library item to a loadout
/// </summary>
public interface IInstallLoadoutItemJob : IJobDefinition<LoadoutItemGroup.ReadOnly>
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
