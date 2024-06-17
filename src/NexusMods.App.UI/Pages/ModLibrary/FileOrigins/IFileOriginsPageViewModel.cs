using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Platform.Storage;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary;

public interface IFileOriginsPageViewModel : IPageViewModelInterface
{
    LoadoutId LoadoutId { get; }

    ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins { get; }

    IObservable<IChangeSet<IFileOriginEntryViewModel, IFileOriginEntryViewModel>> SelectedModsObservable { get; [UsedImplicitly] set; }

    ReadOnlyObservableCollection<IFileOriginEntryViewModel> SelectedModsCollection { get; }
    
    string EmptyLibrarySubtitleText { get; }

    /// <summary>
    /// Add a mod to the loadout using the standard installer.
    /// </summary>
    ReactiveCommand<Unit, Unit> AddMod { get; }
    
    /// <summary>
    /// Add a mod to the loadout using the advanced installer.
    /// </summary>
    ReactiveCommand<Unit, Unit> AddModAdvanced { get; }
    
    /// <summary>
    /// Opens the Nexus mod page.
    /// </summary>
    ReactiveCommand<Unit, Unit> OpenNexusModPage { get; }
    
    /// <summary>
    /// Registers a new mod from disk.
    /// </summary>
    public Task RegisterFromDisk(IStorageProvider storageProvider);
}
