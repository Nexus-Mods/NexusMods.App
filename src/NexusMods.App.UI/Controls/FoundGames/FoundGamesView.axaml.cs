using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls.GameWidget;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.FoundGames;

public partial class FoundGamesView : ReactiveUserControl<IFoundGamesViewModel>
{
    public FoundGamesView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Games)
                .BindTo<ReadOnlyObservableCollection<IGameWidgetViewModel>, FoundGamesView, IEnumerable>(this, view => view.FoundGamesItemsControl.ItemsSource)
                .DisposeWith(d);
        });
    }
}
