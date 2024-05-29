using System.Collections.ObjectModel;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModLibrary;

public interface IFileOriginsPageViewModel : IPageViewModelInterface
{
    LoadoutId LoadoutId { get; }
    
    ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins { get; }
    
    /// <summary>
    /// Add a mod to the loadout using the standard installer.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Task AddMod(string path);

    /// <summary>
    /// Add a mod to the loadout using the advanced installer.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Task AddModAdvanced(string path);
}
