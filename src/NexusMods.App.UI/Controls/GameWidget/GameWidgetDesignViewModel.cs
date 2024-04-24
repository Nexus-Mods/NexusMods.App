using System.Reactive;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.GameLocators;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.GameWidget;

public class GameWidgetDesignViewModel : AViewModel<IGameWidgetViewModel>, IGameWidgetViewModel
{
    [Reactive]
    public GameInstallation Installation { get; set; } = GameInstallation.Empty;
    public string Name { get; } = "SOME CYBERPUNK GAME WITH A LONG NAME";
    public Bitmap Image { get; }
    public ReactiveCommand<Unit,Unit> AddGameCommand { get; set; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> ViewGameCommand { get; set; } = ReactiveCommand.Create(() => { });
    // TODO: This is temporary, to speed up development. Until design comes up with UX for deleting loadouts.
    public ReactiveCommand<Unit, Unit> RemoveAllLoadoutsCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive]
    public GameWidgetState State { get; set; }

    public GameWidgetDesignViewModel()
    {
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        State = GameWidgetState.DetectedGame;
    }
}
