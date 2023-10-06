using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public partial class AdvancedInstallerSelectLocationView : ReactiveUserControl<IAdvancedInstallerSelectLocationViewModel>
{
    public AdvancedInstallerSelectLocationView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.SuggestedEntries)
                .BindTo<ReadOnlyObservableCollection<IAdvancedInstallerSuggestedEntryViewModel>, AdvancedInstallerSelectLocationView, IEnumerable>(this, view => view.SuggestedLocationItemsControl.ItemsSource)
                .DisposeWith(disposables);
        });
    }
}

