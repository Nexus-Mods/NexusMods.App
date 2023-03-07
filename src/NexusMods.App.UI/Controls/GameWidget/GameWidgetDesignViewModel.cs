using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.App.UI.ViewModels;
using NexusMods.DataModel.Games;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.GameWidget;

public class GameWidgetDesignViewModel : AViewModel<IGameWidgetViewModel>, IGameWidgetViewModel
{
    [Reactive]
    public GameInstallation Installation { get; set; }
    public string Name { get; } = "SOME CYBERPUNK GAME WITH A LONG NAME";
    public IImage Image { get; }
    public ICommand PrimaryButton { get; set; }
    public ICommand? SecondaryButton { get; set; }

    public GameWidgetDesignViewModel()
    {
        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        Image = new Bitmap(assets.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/fantasy_game.png")));
        SecondaryButton = ReactiveCommand.Create(() => { });
    }
}
