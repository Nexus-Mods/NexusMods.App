using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.RestoreLoadout;

public partial class RestoreLoadoutView : ReactiveUserControl<IRestoreLoadoutViewModel>
{
    public RestoreLoadoutView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.Revisions, view => view.RevisionsControl.ItemsSource)
                    .DisposeWith(d);
            }
        );
    }
}

