using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage.Dialogs;

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
            
            // this.WhenAnyValue(v => v.ViewModel!.IsListed)
            //     .Subscribe(isListed =>
            //         {
            //             if (isListed)
            //             {
            //                 RadioVisibilityListed.IsChecked = true;
            //                 RadioVisibilityUnlisted.IsChecked = false;
            //             }
            //             else
            //             {
            //                 RadioVisibilityListed.IsChecked = false;
            //                 RadioVisibilityUnlisted.IsChecked = true;
            //             }
            //         }
            //     ).DisposeWith(disposables);
        });
    }
}

