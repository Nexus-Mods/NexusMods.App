using System.Windows.Input;
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
    public ICommand PrimaryButton { get; set; } = Initializers.ICommand;
    public ICommand? SecondaryButton { get; set; }
    
    [Reactive]
    public GameWidgetState State { get; set; }

    public GameWidgetDesignViewModel()
    {
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        SecondaryButton = ReactiveCommand.Create(() => { });
        State = GameWidgetState.DetectedGame;
    }
}
