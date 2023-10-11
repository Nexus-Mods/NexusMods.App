using System.Reactive;
using Avalonia;
using Avalonia.Media;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IAddPanelButtonViewModel : IViewModelInterface
{
    public IReadOnlyDictionary<PanelId, Rect> NewLayoutState { get; }

    public IImage ButtonImage { get; }

    public ReactiveCommand<Unit, Unit> AddPanelCommand { get; }
}
