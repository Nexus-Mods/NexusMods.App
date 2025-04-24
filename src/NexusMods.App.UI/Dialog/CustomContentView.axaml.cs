using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Dialog;

public partial class CustomContentView : ReactiveUserControl<IDialogContentViewModel>
{
    public CustomContentView()
    {
        InitializeComponent();
        
        // Set the ViewModel for the ViewModelViewHost in the code-behind
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, 
                    vm => vm.CloseWindowCommand,
                    view => view.CloseButton.Command)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, 
                    vm => vm.CloseWindowCommand,
                    view => view.UpgradeToPremium.Command)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, 
                    vm => vm.CloseWindowCommand,
                    view => view.DoNothing.Command)
                .DisposeWith(disposables);
            
        });
    }
}

