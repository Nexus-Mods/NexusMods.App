using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LibraryPage.Collections;

public partial class CollectionsView : ReactiveUserControl<ICollectionsViewModel>
{
    public CollectionsView()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.Collections, view => view.Collections.ItemsSource)
                .DisposeWith(d);
        });
    }
}

