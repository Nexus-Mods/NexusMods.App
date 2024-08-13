using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Library;

[UsedImplicitly]
public partial class LibraryView : ReactiveUserControl<ILibraryViewModel>
{
    public LibraryView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Source, view => view.TreeDataGrid.Source)
                .DisposeWith(disposables);
        });
    }
}

