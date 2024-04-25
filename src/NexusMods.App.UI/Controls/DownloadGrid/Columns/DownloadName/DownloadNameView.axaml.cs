using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadName;

public partial class DownloadNameView : ReactiveUserControl<IDownloadNameViewModel>
{
    public DownloadNameView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel!.Name)
                .BindTo<string, DownloadNameView, string>(this, view => view.NameTextBox.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.DataContext)
                .SubscribeWithErrorLogging(logger: default)
                .DisposeWith(d);
        });
    }
}

