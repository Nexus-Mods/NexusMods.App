using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadOrder;

public partial class LoadOrdersPageView : ReactiveUserControl<ILoadOrdersPageViewModel>
{
    public LoadOrdersPageView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(v=> v.ViewModel!.LoadOrderViewModel)
                    .WhereNotNull()
                    .BindToView(this, v => v.LoadOrderView.ViewModel)
                    .DisposeWith(disposables);
            }
        );
    }
}

