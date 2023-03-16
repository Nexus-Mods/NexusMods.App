using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.Home;

public partial class HomeView : ReactiveUserControl<IHomeViewModel>
{
    public HomeView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.FoundGames)
                .BindTo(this, v => v.FoundGamesHost.ViewModel)
                .DisposeWith(d);
        });
    }
}

