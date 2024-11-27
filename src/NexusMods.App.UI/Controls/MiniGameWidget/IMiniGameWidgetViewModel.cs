using Avalonia.Media.Imaging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Controls.MiniGameWidget;

public interface IMiniGameWidgetViewModel : IViewModelInterface
{
    public IGame? Game { get; set; }
    public GameInstallation[]? GameInstallations { get; set; }
    public string Name { get; set; }
    public bool IsFound { get; set; }
    public Bitmap Image { get; }
}
