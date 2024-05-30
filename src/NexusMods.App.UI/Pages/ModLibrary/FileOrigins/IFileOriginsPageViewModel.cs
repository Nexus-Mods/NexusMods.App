using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModLibrary;

public interface IFileOriginsPageViewModel : IPageViewModelInterface
{
    LoadoutId LoadoutId { get; }

    ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins { get; }

    IReadOnlyList<IFileOriginEntryViewModel> SelectedMods { get; set; }

    /// <summary>
    /// Registers a new mod from disk.
    /// </summary>
    public Task RegisterFromDisk(IStorageProvider storageProvider);

    /// <summary>
    /// Opens the Nexus Mod page for the current game.
    /// </summary>
    public Task OpenNexusModPage();
    
    /// <summary>
    /// Add a mod to the loadout using the standard installer.
    /// </summary>
    public Task AddMods();

    /// <summary>
    /// Add a mod to the loadout using the advanced installer.
    /// </summary>
    public Task AddModsAdvanced();
}
