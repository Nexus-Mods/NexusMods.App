using System.Collections.ObjectModel;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.ModLibrary;

public interface IFileOriginsPageViewModel : IPageViewModelInterface
{
    LoadoutId LoadoutId { get; }
    
    ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins { get; }
    
    void Initialize(LoadoutId loadoutId);
}
