using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.MyGames;

public partial class MyGamesView : ReactiveUserControl<IMyGamesViewModel>
{
    public MyGamesView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.FoundGames)
                .BindTo(this, view => view.FoundGamesViewHost.ViewModel)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.AllGames)
                .BindTo(this, view => view.AllGamesViewHost.ViewModel)
                .DisposeWith(d);
        });
    }
}

