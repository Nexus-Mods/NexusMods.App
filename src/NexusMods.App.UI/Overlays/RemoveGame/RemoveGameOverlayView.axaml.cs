using Avalonia.ReactiveUI;
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

