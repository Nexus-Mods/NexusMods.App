using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NexusMods.DataModel.Games;

namespace NexusMods.App.UI.Controls.GameWidget;

public interface IGameWidgetViewModel : IViewModelInterface
{
    public GameInstallation Installation { get; set; }
    public string Name { get; }

    /// <summary>
    /// Please assign <see cref="WriteableBitmap"/> if possible; for faster image blur.
    /// </summary>
    public IImage Image { get; }

    public ICommand PrimaryButton { get; set; }
    public ICommand? SecondaryButton { get; set; }
}
