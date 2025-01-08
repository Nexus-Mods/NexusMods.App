using System.Collections.ObjectModel;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public class CollectionsDesignViewModel : AViewModel<ICollectionsViewModel>, ICollectionsViewModel
{
    public CollectionsDesignViewModel()
    {
        Collections = new ReadOnlyObservableCollection<ICollectionCardViewModel>([
                new CollectionCardDesignViewModel(),
                new CollectionCardDesignViewModel(),
                new CollectionCardDesignViewModel(),
                new CollectionCardDesignViewModel(),
                new CollectionCardDesignViewModel(),
            ]
        );
    }
    public IconValue TabIcon => IconValues.Collections;
    public string TabTitle => "Collections (WIP)";
    public WindowId WindowId { get; set; }
    public WorkspaceId WorkspaceId { get; set; }
    public PanelId PanelId { get; set; }
    public PanelTabId TabId { get; set; }
    public bool CanClose()
    {
        return false;
    }

    public ReadOnlyObservableCollection<ICollectionCardViewModel> Collections { get; }
}
