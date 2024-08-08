using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Extensions;
using ReactiveUI;
using SkiaSharp;

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
                    })
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Image, v => v.GameImage.Source)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Name, v => v.NameTextBlock.Text)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.Version, v => v.VersionTextBlock.Text)
                    .DisposeWith(d);
                
                this.BindCommand(ViewModel, vm => vm.AddGameCommand, v => v.AddGameButton)
                    .DisposeWith(d);
                
                this.BindCommand(ViewModel, vm => vm.RemoveAllLoadoutsCommand, v => v.RemoveGameButton)
                    .DisposeWith(d);
                
                this.BindCommand(ViewModel, vm => vm.ViewGameCommand, v => v.ViewGameButton)
                    .DisposeWith(d);
                
                StoreIcon.Classes.Add(MapGameStoreToIconClass(ViewModel!.Installation.Store));
            }
        );
    }

    /// <summary>
    /// Returns a game store icon class for a given game store. The plain text store and the icon class name aren't always the same.
    /// </summary>
    /// <param name="store">A <see cref="GameStore"/> object</param>
    /// <returns>An icon representing the game store or a question mark icon if not found.</returns>
    private string MapGameStoreToIconClass(GameStore store) => store.Value switch
    {
        "Steam" => "Steam",
        "GOG" => "GOG",
        "Epic Games Store" => "Epic",
        "Origin" => "Ubisoft",
        "EA Desktop" => "EA",
        "Xbox Game Pass" => "Xbox",
        _ => "Help",
    };
}
