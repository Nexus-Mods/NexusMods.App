using System.Reactive;
using Avalonia.Media.Imaging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.GameWidget;

public interface IGameWidgetViewModel : IViewModelInterface
{
    public GameInstallation Installation { get; set; }
    public string Name { get; }
    public string Version { get; }
    public string Store { get; }
    public IconValue GameStoreIcon { get; }
    public Bitmap Image { get; }
    public ReactiveCommand<Unit, Unit> AddGameCommand { get; set; }
    public ReactiveCommand<Unit, Unit> ViewGameCommand { get; set; }
    public ReactiveCommand<Unit, Unit> RemoveAllLoadoutsCommand { get; set; }
    public IObservable<bool> IsManagedObservable { get; set; }
    public GameWidgetState State { get; set; }
}
