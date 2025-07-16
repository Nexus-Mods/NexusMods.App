using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Resources;
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
            
            UnlistedTitleText.Text = Language.Loadout_Dialog_ShareCollection_UnlistedTitle;
            UnlistedExplanationText.Text = Language.Loadout_Dialog_ShareCollection_UnlistedExplanation;
            ListedTitleText.Text = Language.Loadout_Dialog_ShareCollection_ListedTitle;
            ListedExplanationText.Text = Language.Loadout_Dialog_ShareCollection_ListedExplanation;
            ChangeVisibilityText.Text = Language.Loadout_Dialog_ShareCollection_ChangeVisibilityMessage;
        });
    }
}

