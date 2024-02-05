using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent;

public partial class FoundGamesView : ReactiveUserControl<IFoundGamesViewModel>
{
    public FoundGamesView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Games)
                .BindTo(this, view => view.FoundGamesItemsControl.ItemsSource)
                .DisposeWith(d);
        });
    }
}
