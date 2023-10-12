using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public partial class SelectLocationView : ReactiveUserControl<ISelectLocationViewModel>
{
    public SelectLocationView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.SuggestedEntries)
                .BindTo<ReadOnlyObservableCollection<ISuggestedEntryViewModel>,
                    SelectLocationView, IEnumerable>(this,
                    view => view.SuggestedLocationItemsControl.ItemsSource)
                .DisposeWith(disposables);
        });
    }
}
