using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Image;

public class ImageButtonDesignViewModel : ImageButtonViewModel
{
    public ImageButtonDesignViewModel()
    {
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
        Click = ReactiveCommand.Create(() => { IsActive = !IsActive; });
        Name = "Image Text";
        Tag = new object();
    }
}
