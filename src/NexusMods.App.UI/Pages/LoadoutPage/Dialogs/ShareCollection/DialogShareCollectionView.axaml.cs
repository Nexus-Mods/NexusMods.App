using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage.Dialogs.ShareCollection;

public partial class DialogShareCollectionView : ReactiveUserControl<IDialogShareCollectionViewModel>
{
    public DialogShareCollectionView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {   
            this.Bind(ViewModel,
                vm => vm.IsListed,
                v => v.RadioVisibilityListed.IsChecked
            ).DisposeWith(disposables);
        });
    }
}

