using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.DataModel.Games;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.GameWidget;

public class GameWidgetDesignViewModel : AViewModel<IGameWidgetViewModel>, IGameWidgetViewModel
{
    [Reactive]
    public GameInstallation Installation { get; set; } = GameInstallation.Empty;
    public string Name { get; } = "SOME CYBERPUNK GAME WITH A LONG NAME";
    public IImage Image { get; set; }
    public ICommand PrimaryButton { get; set; } = Initializers.ICommand;
    public ICommand? SecondaryButton { get; set; }

    public GameWidgetDesignViewModel()
    {
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        SecondaryButton = ReactiveCommand.Create(() => { });
    }
}
