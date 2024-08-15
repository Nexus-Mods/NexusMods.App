using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage;

[UsedImplicitly]
public partial class LoadoutView : ReactiveUserControl<ILoadoutViewModel>
{
    public LoadoutView()
    {
        InitializeComponent();

        // this.WhenActivated(disposables =>
        // {
        //     this.OneWayBind(ViewModel, vm => vm.Source, view => view.TreeDataGrid.Source)
        //         .DisposeWith(disposables);
        // });
    }
}

