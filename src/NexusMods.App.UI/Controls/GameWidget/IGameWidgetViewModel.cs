using System.Windows.Input;
using Avalonia.Media.Imaging;
using NexusMods.Abstractions.Games;

namespace NexusMods.App.UI.Controls.GameWidget;

public interface IGameWidgetViewModel : IViewModelInterface
{
    public GameInstallation Installation { get; set; }
    public string Name { get; }
    public Bitmap Image { get; }
    public ICommand PrimaryButton { get; set; }
    public ICommand? SecondaryButton { get; set; }
}
