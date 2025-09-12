using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.App.UI.Controls;
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
        });
    }
}