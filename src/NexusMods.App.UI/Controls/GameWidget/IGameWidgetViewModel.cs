using System.Reactive;
using Avalonia.Media.Imaging;
using NexusMods.Abstractions.GameLocators;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.GameWidget;

public interface IGameWidgetViewModel : IViewModelInterface
{
    public GameInstallation Installation { get; set; }
    public string Name { get; }
    public Bitmap Image { get; }
    public ReactiveCommand<Unit,Unit> AddGameCommand { get; set; }

    public GameWidgetState State { get; set; }
}

public enum GameWidgetState : byte
{
    DetectedGame,
    AddingGame,
    ManagedGame,
    RemovingGame,
}

