using System.Reactive;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.MiniGameWidget;

public class MiniGameWidgetDesignViewModel : AViewModel<IMiniGameWidgetViewModel>, IMiniGameWidgetViewModel
{
    [Reactive]
    public IGame? Game { get; set; }
    public string Name { get; set; } = "Cyberpunk 2077";
    public bool IsFound { get; set;  } = true;
    public Bitmap Image { get; set; }
    public bool EmptyState { get; set; } = false;


    public MiniGameWidgetDesignViewModel()
    {
        var random = new Random();
        
        IsFound = random.Next(2) == 1;
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
    }
}
