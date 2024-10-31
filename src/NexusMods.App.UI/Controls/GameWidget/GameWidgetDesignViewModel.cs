using System.Reactive;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.GameWidget;

public class GameWidgetDesignViewModel : AViewModel<IGameWidgetViewModel>, IGameWidgetViewModel
{
    [Reactive]
    public GameInstallation Installation { get; set; } = new GameInstallation() { Store = GameStore.XboxGamePass, Version = new Version(1,0,0) };
    public string Name { get; } = "SOME CYBERPUNK GAME WITH A LONG NAME";
    public string Version { get; set; }
    public string Store { get; set; }
    public IconValue GameStoreIcon { get; set; }
    public Bitmap Image { get; }
    public ReactiveCommand<Unit,Unit> AddGameCommand { get; set; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> ViewGameCommand { get; set; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> RemoveAllLoadoutsCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive]
    public GameWidgetState State { get; set; }

    public GameWidgetDesignViewModel()
    {
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        State = GameWidgetState.ManagedGame;
        
        Version = $"Version: {Installation.Version}";
        Store = Installation.Store.Value;
        GameStoreIcon = GameWidgetViewModel.MapGameStoreToIcon(Installation.Store);
    }
}
