using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModLibrary;

public interface IFileOriginsPageViewModel : IPageViewModelInterface
{
    LoadoutId LoadoutId { get; }

    ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins { get; }

    IObservable<IChangeSet<IFileOriginEntryViewModel, IFileOriginEntryViewModel>> SelectedModsObservable { get; [UsedImplicitly] set; }

    ReadOnlyObservableCollection<IFileOriginEntryViewModel> SelectedModsCollection { get; }
    
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
    Task AddMod();
    
    /// <summary>
    /// Add a mod to the loadout using the advanced installer.
    /// </summary>
    Task AddModAdvanced();
}
