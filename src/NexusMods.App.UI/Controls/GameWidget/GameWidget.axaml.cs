using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Extensions;
using NexusMods.Icons;
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
                        }
                    )
                    .DisposeWith(d);


                this.OneWayBind(ViewModel, vm => vm.Image, v => v.GameImage.Source)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Name, v => v.NameTextBlock.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.Version, v => v.VersionTextBlock.Text)
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.GameStoreIcon, view => view.GameStoreIcon.Value,
                        image => MapGameStoreToIconClass(ViewModel!.Installation.Store)
                    )
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


    /// <summary>
    /// Returns an <see cref="IconValue"/> for a given <see cref="GameStore"/>.
    /// </summary>
    /// <param name="store">A <see cref="GameStore"/> object</param>
    /// <returns>An <see cref="IconValue"/> icon representing the game store or a question mark icon if not found.</returns>
    private IconValue MapGameStoreToIconClass(GameStore store)
    {
        if (store == GameStore.Steam)
            return IconValues.Steam;
        else if (store == GameStore.GOG)
            return IconValues.GOG;
        else if (store == GameStore.EGS)
            return IconValues.Epic;
        else if (store == GameStore.Origin)
            return IconValues.Ubisoft;
        else if (store == GameStore.EADesktop)
            return IconValues.EA;
        else if (store == GameStore.XboxGamePass)
            return IconValues.Xbox;

        return IconValues.Help;
    }
}
