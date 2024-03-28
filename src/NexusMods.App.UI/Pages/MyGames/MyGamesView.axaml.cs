using System.Reactive.Disposables;
using System.Reactive.Linq;
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
            
            this.WhenAnyValue(view => view.ViewModel!.DetectedGames.Count)
                .Select(count => count == 0)
                .BindToView(this, view => view.NoGamesDetectedTextBlock.IsVisible)
                .DisposeWith(d);
            
            this.WhenAnyValue(view => view.ViewModel!.ManagedGames.Count)
                .Select(count => count == 0)
                .BindToView(this, view => view.NoGamesManagedTextBlock.IsVisible)
                .DisposeWith(d);
        });
    }
}

