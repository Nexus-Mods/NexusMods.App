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
                this.WhenAnyValue(view => view.ViewModel!.InstalledGames)
                    .BindToView(this, view => view.DetectedGamesItemsControl.ItemsSource)
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel!.SupportedGames)
                    .BindToView(this, view => view.SupportedGamesItemsControl.ItemsSource)
                    .DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.GiveFeedbackCommand, view => view.GiveFeedbackButton)
                    .DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.OpenRoadmapCommand, view => view.OpenRoadmapButton)
                    .DisposeWith(d);
                
                this.WhenAnyValue(view  => view.ViewModel!.InstalledGames.Count)
                    .Select(installedCount  => installedCount == 0)
                    .Subscribe(isEmpty =>
                        {
                            NoGamesDetectedText.IsVisible = isEmpty;
                            AddGamesToGetStartedText.IsVisible = !isEmpty;
                            DetectedGamesItemsControl.IsVisible = !isEmpty;
                        }
                    )
                    .DisposeWith(d);
            }
        );
    }
}
