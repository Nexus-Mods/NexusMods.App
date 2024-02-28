using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ViewModInfo;

public partial class ViewModInfoView : ReactiveUserControl<IViewModInfoViewModel>
{
    public ViewModInfoView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.PageViewModel, view => view.PageViewHost.ViewModel)
                .DisposeWith(d);
        });
    }
}

