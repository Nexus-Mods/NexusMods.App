using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Search;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Downloads;

[UsedImplicitly]
public partial class DownloadsPageView : ReactiveUserControl<IDownloadsPageViewModel>
{
    public DownloadsPageView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<DownloadsPageView, IDownloadsPageViewModel, CompositeItemModel<DownloadId>, DownloadId>(
            this,
            TreeDataGridDownloads,
            vm => vm.Adapter
        );

        this.WhenActivated(disposables =>
        {
            // Bind TreeDataGrid Source
            this.OneWayBind(ViewModel,
                    vm => vm.Adapter.Source.Value,
                    view => view.TreeDataGridDownloads.Source
                )
                .DisposeWith(disposables);

            // Add SearchControl keyboard handlers and adapter binding
            SearchControl.AttachKeyboardHandlers(this, disposables);
            this.OneWayBind(ViewModel, vm => vm.Adapter, view => view.SearchControl.Adapter)
                .DisposeWith(disposables);
            
            // Bind command buttons
            this.BindCommand(ViewModel, vm => vm.PauseAllCommand, view => view.PauseAllButton)
                .DisposeWith(disposables);
                
            this.BindCommand(ViewModel, vm => vm.ResumeAllCommand, view => view.ResumeAllButton)
                .DisposeWith(disposables);
                
            this.BindCommand(ViewModel, vm => vm.PauseSelectedCommand, view => view.PauseSelectedButton)
                .DisposeWith(disposables);
                
            this.BindCommand(ViewModel, vm => vm.ResumeSelectedCommand, view => view.ResumeSelectedButton)
                .DisposeWith(disposables);
                
            this.BindCommand(ViewModel, vm => vm.CancelSelectedCommand, view => view.CancelSelectedButton)
                .DisposeWith(disposables);
        });
    }
}
