using System.Reactive;
using Avalonia.Media;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IAddPanelButtonViewModel : IViewModelInterface
{
    public WorkspaceGridState NewLayoutState { get; }

    public IImage ButtonImage { get; }

    public ReactiveCommand<Unit, WorkspaceGridState> AddPanelCommand { get; }
}
