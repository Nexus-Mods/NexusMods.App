using System.Reactive;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.MiniGameWidget;

public class MiniGameWidgetDesignViewModel : AViewModel<IMiniGameWidgetViewModel>, IMiniGameWidgetViewModel
{
    [Reactive]
    public GameInstallation Installation { get; set; } = new GameInstallation() { Store = GameStore.XboxGamePass, Version = new Version(1,2,3) };
    
    public string Name { get; } = "Cyberpunk 2077";
    public string Store { get; set; }
    public IconValue GameStoreIcon { get; set; }
    public Bitmap Image { get; }
    
    [Reactive]
    public GameWidgetState State { get; set; }

    [Reactive] public bool Placeholder { get; set; } = false;

    public MiniGameWidgetDesignViewModel()
    {
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        State = GameWidgetState.ManagedGame;
        Store = Installation.Store.Value;
        GameStoreIcon = MiniGameWidgetViewModel.MapGameStoreToIcon(Installation.Store);
    }
}
