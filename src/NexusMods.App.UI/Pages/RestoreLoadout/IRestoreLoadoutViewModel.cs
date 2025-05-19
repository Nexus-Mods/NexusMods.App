using System.Collections.ObjectModel;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.DataModel.Undo;

namespace NexusMods.App.UI.Pages.RestoreLoadout;


public interface IRestoreLoadoutViewModel : IPageViewModelInterface
{
    public LoadoutId LoadoutId { get; set; }
    
    public ReadOnlyObservableCollection<IRevisionViewModel> Revisions { get; }
}
