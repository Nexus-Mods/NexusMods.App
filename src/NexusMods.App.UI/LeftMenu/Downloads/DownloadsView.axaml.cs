using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Extensions;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Downloads;

public partial class DownloadsView : ReactiveUserControl<IDownloadsViewModel>
{
    public DownloadsView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            ViewModel!.IsActive(Options.InProgress)
                .BindToActive(InProgressButton)
                .DisposeWith(d);
            ViewModel!.IsActive(Options.Completed)
                .BindToActive(CompletedButton)
                .DisposeWith(d);
            ViewModel!.IsActive(Options.History)
                .BindToActive(HistoryButton)
                .DisposeWith(d);

            // TODO: Information of this kind is to be moved into UI guidelines doc. This is just a temporary reference for myself, and other contributors
            
            // The way this works is CommandFor(ViewModel) will fire IViewModelSelector's `Set` method
            // which will in turn change DownloadsViewModel->ViewModelSelector->AViewModelSelector->CurrentViewModel. 
            
            // Change in CurrentViewModel will emit a change in DownloadsViewModel.RightContent
            // (via binding) set in DownloadsViewModel.
            
            // That in turn will update MainWindowViewModel's RightContent, due to the bind: 
            // this.OneWayBind(ViewModel, vm => vm.RightContent, v => v.RightContent.ViewModel) in  
            
            // Then, InjectedViewLocator.ResolveView will be fired (which we register at startup), by ReactiveUI
            // to resolve the view.

            InProgressButton.Command = ViewModel.CommandForViewModel(Options.InProgress);
            CompletedButton.Command = ViewModel.CommandForViewModel(Options.Completed);
            HistoryButton.Command = ViewModel.CommandForViewModel(Options.History);
        });


    }
}

