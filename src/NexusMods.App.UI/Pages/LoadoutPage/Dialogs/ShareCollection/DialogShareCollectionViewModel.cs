using NexusMods.UI.Sdk;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutPage.Dialogs.ShareCollection;

public interface IDialogShareCollectionViewModel: IViewModelInterface
{
    bool IsListed { get; set; }
}

public class DialogShareCollectionViewModel : AViewModel<IDialogShareCollectionViewModel>, IDialogShareCollectionViewModel
{
    [Reactive] public bool IsListed { get; set; }

    public DialogShareCollectionViewModel(bool isVisible)
    {
        IsListed = isVisible;
    }
}
