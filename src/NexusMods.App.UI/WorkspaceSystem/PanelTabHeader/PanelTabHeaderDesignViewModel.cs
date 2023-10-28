using System.Reactive;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabHeaderDesignViewModel : AViewModel<IPanelTabHeaderViewModel>, IPanelTabHeaderViewModel
{
    public PanelTabId Id { get; } = PanelTabId.From(Guid.Empty);

    public string Title { get; set; } = "Very long name for tab headers";

    public IImage? Icon { get; set; }

    [Reactive]
    public bool IsSelected { get; set; }

    public ReactiveCommand<Unit, Unit> CloseTabCommand => Initializers.EnabledReactiveCommand;

    public PanelTabHeaderDesignViewModel()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png"));
            Icon = new Bitmap(stream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
