using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Pages.LoadoutPage.Dialogs.CollectionPublished;

public interface IDialogCollectionPublishedViewModel: IViewModelInterface
{
    string CollectionName { get;  }
    CollectionStatus CollectionStatus { get;  }
    Uri CollectionUrl { get;  }
}

public class DialogCollectionPublishedViewModel : AViewModel<IDialogCollectionPublishedViewModel>, IDialogCollectionPublishedViewModel
{
    public string CollectionName { get; }
    public CollectionStatus CollectionStatus { get; set; }
    public Uri CollectionUrl { get;  }
    
    public DialogCollectionPublishedViewModel(string collectionName, CollectionStatus collectionStatus, Uri collectionUrl)
    {
        CollectionName = collectionName;
        CollectionUrl = collectionUrl;
        CollectionStatus = collectionStatus;
    }
}
