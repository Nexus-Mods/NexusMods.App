using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Extensions;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.GameWidget;

public partial class GameWidget : ReactiveUserControl<IGameWidgetViewModel>
{
    private GameWidgetState _previousState;

    public GameWidget()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.WhenAnyValue(view => view.ViewModel!.State)
                    .Subscribe(state =>
                        {
                            DetectedGameStackPanel.IsVisible = state == GameWidgetState.DetectedGame;
                            AddingGameStackPanel.IsVisible = state == GameWidgetState.AddingGame;
                            ManagedGameGrid.IsVisible = state == GameWidgetState.ManagedGame;
                            RemovingGameStackPanel.IsVisible = state == GameWidgetState.RemovingGame;
                            GameWidgetBorder.Classes.ToggleIf("Disabled", state is GameWidgetState.AddingGame or GameWidgetState.RemovingGame);
                        }
                    )
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.Store)
                    .Subscribe(store =>
                        {
                            // Update the tooltip text for the game store
                            ToolTip.SetTip(StoreBackground, store);
                        }
                    )
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.Name)
                    .Subscribe(name =>
                        {
                            // Update the tooltip for the game name
                            ToolTip.SetTip(ImageSectionBorder, name);
                        }
                    )
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Image, v => v.GameImage.Source)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Version, v => v.VersionTextBlock.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.GameStoreIcon, v => v.GameStoreIcon.Value)
                    .DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.AddGameCommand, v => v.AddGameButton)
                    .DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.RemoveAllLoadoutsCommand, v => v.RemoveGameButton)
                    .DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.ViewGameCommand, v => v.ViewGameButton)
                    .DisposeWith(d);
            }
        );
    }
}
