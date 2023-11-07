using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
public partial class SelectLocationView : ReactiveUserControl<ISelectLocationViewModel>
{
    public SelectLocationView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.SuggestedEntries)
                .BindTo<ReadOnlyObservableCollection<ISuggestedEntryViewModel>, SelectLocationView, IEnumerable>(this, view => view.SuggestedLocationItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind<ISelectLocationViewModel, SelectLocationView, ReadOnlyObservableCollection<ISelectLocationTreeViewModel>, IEnumerable?>(ViewModel, vm => vm.AllFoldersTrees,
                view => view.AllFoldersItemsControl.ItemsSource).DisposeWith(disposables);
        });
    }
}
