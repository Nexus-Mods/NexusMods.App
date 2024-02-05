using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadGameName;

public partial class DownloadGameNameView : ReactiveUserControl<IDownloadGameNameViewModel>
{
    public DownloadGameNameView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel!.Game)
                .BindToUi<string, DownloadGameNameView, string>(this, view => view.GameTextBox.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.DataContext)
                .SubscribeWithErrorLogging(logger: default)
                .DisposeWith(d);
        });
    }
}

