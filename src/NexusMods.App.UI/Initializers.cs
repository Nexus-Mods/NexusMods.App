using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.LeftMenu.Home;
using NexusMods.App.UI.RightContent;
using NexusMods.DataModel.Games;
using ReactiveUI;
// ReSharper disable InconsistentNaming

namespace NexusMods.App.UI;

public static class Initializers
{
    public static readonly ICommand ICommand  = ReactiveCommand.Create(() => { });
    public static readonly IImage IImage = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Opaque);
    public static readonly ILeftMenuViewModel ILeftMenuViewModel = new HomeLeftMenuDesignViewModel();
    public static readonly IRightContent IRightContent = new FoundGamesDesignViewModel();

    public static ReadOnlyObservableCollection<T> ReadOnlyObservableCollection<T>()
    {
        return new ReadOnlyObservableCollection<T>(new ObservableCollection<T>());
    }
}
