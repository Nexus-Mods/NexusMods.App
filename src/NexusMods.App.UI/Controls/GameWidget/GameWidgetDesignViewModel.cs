using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.GameWidget;

public class GameWidgetDesignViewModel : AViewModel<IGameWidgetViewModel>, IGameWidgetViewModel
{
    
    [Reactive]
    public GameInstallation Installation { get; set; } = new() { Store = GameStore.XboxGamePass};
    public string Name { get; } = "Cyberpunk 2077";
    public string Version { get; set; }
    public string Store { get; set; }
    public IconValue GameStoreIcon { get; set; }
    public Bitmap Image { get; }
    public ReactiveCommand<Unit,Unit> AddGameCommand { get; set; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> ViewGameCommand { get; set; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> RemoveAllLoadoutsCommand { get; set; } = ReactiveCommand.Create(() => { });
    
    public IObservable<bool> IsManagedObservable { get; set; } = Observable.Return(false);

    [Reactive]
    public GameWidgetState State { get; set; }
 
    public GameWidgetDesignViewModel()
    {
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        State = GameWidgetState.DetectedGame;
        
        Version = $"Version: 1.5.6";
        Store = Installation.Store.Value;
        GameStoreIcon = GameWidgetViewModel.MapGameStoreToIcon(Installation.Store);
    }
}
