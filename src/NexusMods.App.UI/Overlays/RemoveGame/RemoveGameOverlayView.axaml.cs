using Avalonia.ReactiveUI;
using Humanizer;
using Humanizer.Bytes;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays;

public partial class RemoveGameOverlayView : ReactiveUserControl<IRemoveGameOverlayViewModel>
{
    public RemoveGameOverlayView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Subscribe(viewModel =>
                {
                    GameName.Text = viewModel.GameName;
                    NumDownloads.Text = viewModel.NumDownloads.ToString("N0");
                    SumDownloadsSize.Text = ByteSize.FromBytes(viewModel.SumDownloadsSize.Value).Humanize();
                    NumCollections.Text = viewModel.NumCollections.ToString("N0");
                })
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.GameName, view => view.GameName.Text)
                .AddTo(disposables);

            this.Bind(ViewModel, vm => vm.ShouldDeleteDownloads.Value, view => view.SwitchDeleteDownloads.IsChecked)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandCancel, view => view.ButtonCancel)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandRemove, view => view.ButtonRemove)
                .AddTo(disposables);
        });
    }
}

