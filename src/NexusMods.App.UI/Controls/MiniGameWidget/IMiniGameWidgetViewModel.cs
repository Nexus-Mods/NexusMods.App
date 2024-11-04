using System.Reactive;
using Avalonia.Media.Imaging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.MiniGameWidget;

public interface IMiniGameWidgetViewModel : IViewModelInterface
{
    public GameInstallation Installation { get; set; }
    public string Name { get; }
    public string Store { get; }
    public Bitmap Image { get; }
    public GameWidgetState State { get; set; }
    public bool Placeholder { get; }
}
