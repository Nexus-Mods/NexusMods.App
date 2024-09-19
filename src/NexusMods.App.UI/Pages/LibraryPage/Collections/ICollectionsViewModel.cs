using System.Collections.ObjectModel;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public interface ICollectionsViewModel : IPageViewModelInterface
{
    public ReadOnlyObservableCollection<ICollectionCardViewModel> Collections { get; }
}
