using NexusMods.Abstractions.UI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.LoadoutPage.Dialogs;

public interface IDialogShareCollectionViewModel: IViewModelInterface
{
    bool IsListed { get; set; }
}

public class DialogShareCollectionViewModel : AViewModel<IDialogShareCollectionViewModel>, IDialogShareCollectionViewModel
{
    [Reactive] public bool IsListed { get; set; }

    public DialogShareCollectionViewModel(bool isListed)
    {
        IsListed = isListed;
    }
}
