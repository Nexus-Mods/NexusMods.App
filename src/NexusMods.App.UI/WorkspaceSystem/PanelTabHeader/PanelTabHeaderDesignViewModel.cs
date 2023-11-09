using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabHeaderDesignViewModel : PanelTabHeaderViewModel
{
    public PanelTabHeaderDesignViewModel() : base(PanelTabId.Empty)
    {
        Title = "Very long name for tab headers";

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
