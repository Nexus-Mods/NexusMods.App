using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyGames;

public partial class MyGamesView : ReactiveUserControl<IMyGamesViewModel>
{
    public MyGamesView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            
            this.WhenAnyValue(view => view.ViewModel!.ManagedGames)
                .BindToView(this, view => view.ManagedGamesItemsControl.ItemsSource)
                .DisposeWith(d);
            
            this.WhenAnyValue(view => view.ViewModel!.DetectedGames)
                .BindToView(this, view => view.DetectedGamesItemsControl.ItemsSource)
                .DisposeWith(d);
        });
    }
}

